// Copyright 2023 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1.Tasks;
using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// This class uses TaskHelper.ConfigureAwait, rather than directly calling .ConfigureAwait().
// When running in a non-test environment this indirectly calls .ConfigureAwait(false).
// Disable the ConfigureAwaitChecker warning:
#pragma warning disable CAC002

namespace Google.Cloud.PubSub.V1;

public sealed partial class SubscriberClientImpl
{
    /// <summary>
    /// Controls a single <see cref="ChannelBase"/>/<see cref="SubscriberClient"/> within this
    /// <see cref="SubscriberClientImpl"/>. This class controls the pulling of messages, and
    /// the pushing of message acks and lease-extensions back to the server.
    /// It also manages error conditions within the channel, restarting RPCs as required.
    /// </summary>
    private class SingleChannel
    {
        private readonly struct NextAction
        {
            internal bool IsPull { get; }
            internal Action Action { get; }
            
            internal NextAction(bool isPull, Action action)
            {
                IsPull = isPull;
                Action = action;
            }
        }

        private readonly struct TaskNextAction
        {
            internal Task Task { get; }
            internal NextAction NextAction { get; }
            
            internal TaskNextAction(Task task, NextAction nextAction)
            {
                Task = task;
                NextAction = nextAction;
            }            
        }

        private readonly struct TimedId // "Time" is abstract, a monotonic incrementing counter is used.
        {
            internal long Time { get; }
            internal string Id { get; }
            
            internal TimedId(long time, string id)
            {
                Time = time;
                Id = id;
            }           
        }

        /// <summary>
        /// This struct contains the retry information for a temporary failed AckId/NackId/ExtendId.
        /// The AckId/NackId/ExtendId is stored in a dictionary as a key and mapped to <see cref="RetryInfo"/>.
        /// This struct is only used in exactly once delivery mode.
        /// </summary>
        private readonly struct RetryInfo
        {
            /// <summary>
            /// Gets the first time of failure in UTC when the Acknowledge/ModifyAckDeadline RPC call failed with a temporary error for a given message id.
            /// </summary>
            internal DateTime FirstTimeOfFailureInUtc { get; }

            /// <summary>
            /// Gets the latest backoff that was used to retry Acknowledge/ModifyAckDeadline RPC call.
            /// </summary>
            internal TimeSpan? Backoff { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="RetryInfo"/> struct.
            /// </summary>
            /// <param name="firstTimeOfFailureInUtc">The first time of failure in UTC.</param>
            /// <param name="backoff">The latest backoff that was used, which is null at the time of first temporary failure.</param>
            internal RetryInfo(DateTime firstTimeOfFailureInUtc, TimeSpan? backoff = null)
            {
                FirstTimeOfFailureInUtc = firstTimeOfFailureInUtc;
                Backoff = backoff;
            }

            internal RetryInfo WithBackoff(TimeSpan? backoff) => new RetryInfo(FirstTimeOfFailureInUtc, backoff);
        }
        
        
        private static readonly RetrySettings s_pullBackoff = RetrySettings.FromExponentialBackoff(
            maxAttempts: int.MaxValue,
            initialBackoff: TimeSpan.FromSeconds(0.5),
            maxBackoff: TimeSpan.FromSeconds(30),
            backoffMultiplier: 2.0,
            retryFilter: _ => false // Ignored
            );

        private static readonly RetrySettings s_ackBackoff = RetrySettings.FromExponentialBackoff(
            maxAttempts: int.MaxValue,
            initialBackoff: TimeSpan.FromSeconds(1),
            maxBackoff: TimeSpan.FromSeconds(64),
            backoffMultiplier: 2.0,
            retryFilter: _ => false // Ignored
            );

        // Stream shutdown occurs after 1 minute, so ensure we're always before that.
        private static readonly TimeSpan s_streamPingPeriod = TimeSpan.FromSeconds(25);

        // This dictionary will only have ids that can be retried for temporary errors while calling Acknowledgement or ModifyAckDeadline RPCs.
        // We store the first time of error corresponding to the AckId, so that we retry only for specified duration
        // which is a requirement for exactly once delivery subscription.
        private readonly ConcurrentDictionary<string, RetryInfo> _retryableIds = new ConcurrentDictionary<string, RetryInfo>();
        private readonly object _lock = new object(); // For: _ackQueue, _nackQueue, _userHandlerInFlight
        private readonly Action<Task> _registerTaskFn;
        private readonly TaskHelper _taskHelper;
        private readonly IScheduler _scheduler;
        private readonly IClock _clock;
        private readonly SubscriberServiceApiClient _client;
        private readonly SubscriptionHandler _handler;
        private readonly CancellationTokenSource _hardStopCts;
        private readonly CancellationTokenSource _pushStopCts;
        private readonly CancellationTokenSource _softStopCts;
        private readonly SubscriptionName _subscriptionName;
        private readonly int _modifyDeadlineSeconds; // Seconds to add to deadling on lease extension.
        private readonly TimeSpan _autoExtendInterval; // Delay between auto-extends for non-exactly once delivery subscriptions.
        private readonly TimeSpan _autoExtendIntervalForExactlyOnceDelivery; // Delay between auto-extends for exactly once delivery subscriptions.
        private readonly TimeSpan _maxExtensionDuration; // Maximum duration for which a message lease will be extended.
        private readonly TimeSpan _extendQueueThrottleInterval; // Throttle pull if items in the extend queue are older than this.
        private readonly int _maxAckExtendQueueSize; // Soft limit on push queue sizes. Used to throttle pulls.
        private readonly int _maxAckExtendSendCount; // Maximum number of ids to include in an ack/nack/extend push RPC.
        private readonly int _maxConcurrentPush; // Mamimum number (slightly soft) of concurrent ack/nack/extend push RPCs.

        private readonly Flow _flow;
        private readonly bool _useLegacyFlowControl;
        private readonly AsyncAutoResetEvent _eventPush;
        private readonly AsyncSingleRecvQueue<TaskNextAction> _continuationQueue;
        private readonly RequeueableQueue<TimedId> _extendQueue = new RequeueableQueue<TimedId>();
        private readonly RequeueableQueue<string> _ackQueue = new RequeueableQueue<string>();
        private readonly RequeueableQueue<string> _nackQueue = new RequeueableQueue<string>();

        private int _pushInFlight = 0;
        private int _userHandlerInFlight = 0;
        private SubscriberServiceApiClient.StreamingPullStream _pull = null;
        private int _concurrentPushCount = 0;
        private bool _pullComplete = false;
        private long _extendThrottleHigh = 0; // Incremented on extension, and put on extend queue items.
        private long _extendThrottleLow = 0; // Incremented after _extendQueueThrottleInterval, checked when throttling.
        private bool _exactlyOnceDeliveryEnabled = false; // True if subscription is exactly once, else false.
        private TimeSpan? _pullBackoff = null;
        
        internal SingleChannel(SubscriberClientImpl subscriber,
            SubscriberServiceApiClient client, SubscriptionHandler handler,
            Flow flow, bool useLegacyFlowControl,
            Action<Task> registerTaskFn)
        {
            _registerTaskFn = registerTaskFn;
            _taskHelper = subscriber._taskHelper;
            _scheduler = subscriber._scheduler;
            _clock = subscriber._clock;
            _client = client;
            _handler = handler;
            _hardStopCts = subscriber._globalHardStopCts;
            _pushStopCts = CancellationTokenSource.CreateLinkedTokenSource(_hardStopCts.Token);
            _softStopCts = subscriber._globalSoftStopCts;
            _subscriptionName = subscriber.SubscriptionName;
            _modifyDeadlineSeconds = subscriber._modifyDeadlineSeconds;
            _maxAckExtendQueueSize = subscriber._maxAckExtendQueue;
            _autoExtendInterval = subscriber._autoExtendInterval;
            _autoExtendIntervalForExactlyOnceDelivery = subscriber._autoExtendIntervalForExactlyOnceDelivery;
            _maxExtensionDuration = subscriber._maxExtensionDuration;
            _extendQueueThrottleInterval = TimeSpan.FromTicks((long)((TimeSpan.FromSeconds(_modifyDeadlineSeconds) - _autoExtendInterval).Ticks * 0.5));
            _maxAckExtendSendCount = Math.Max(10, subscriber._maxAckExtendQueue / 4);
            _maxConcurrentPush = 3; // Fairly arbitrary.
            _flow = flow;
            _useLegacyFlowControl = useLegacyFlowControl;
            _eventPush = new AsyncAutoResetEvent(subscriber._taskHelper);
            _continuationQueue = new AsyncSingleRecvQueue<TaskNextAction>(subscriber._taskHelper);
        }

        internal async Task StartAsync()
        {
            // Start pull.
            StartStreamingPull();
            // Start push.
            HandlePush();
            // Start stream-keep-alive ping
            HandleStreamPing();
            // Start event loop.
            // This loop exits by an action throwing a error or cancellation exception.
            while (!(_pullComplete && IsPushComplete()))
            {
                // Wait for, then process next continuation.
                TaskNextAction nextContinuation = await _taskHelper.ConfigureAwait(_continuationQueue.DequeueAsync());
                // On hardstop just immediately stop this event loop.
                // The registered-task code ensures that all currently-active tasks finish before
                // return to user code.
                if (_hardStopCts.IsCancellationRequested)
                {
                    StopStreamingPull();
                    throw new OperationCanceledException();
                }
                var task = nextContinuation.Task;
                var next = nextContinuation.NextAction;
                if (next.IsPull && (task.IsCanceled || (task.IsFaulted && (task.Exception.IsCancellation() || task.Exception.IsRpcCancellation()))))
                {
                    // Pull has been cancelled by user, shutdown pull stream and don't run continuation.
                    // RPC exceptions are dealt with in the relevant handlers.
                    StopStreamingPull();
                    // Pull process has been stopped, wait for push process to complete.
                    _pullComplete = true;
                }
                else
                {
                    next.Action();
                }
            }
            // Stop waiting for data to push.
            _pushStopCts.Cancel();
        }

        private bool IsPushComplete()
        {
            // extend-queue not included, as these have no effect after shutdown.
            // Lock required for ackQueue and nackQueue.
            lock (_lock)
            {
                return _ackQueue.Count == 0 && _nackQueue.Count == 0 && _pushInFlight == 0 && _userHandlerInFlight == 0;
            }
        }

        private NextAction Next(bool isPull, Action action) => new NextAction(isPull, action);

        private void Add(Task task, NextAction next)
        {
            _registerTaskFn(task);
            var taskNext = new TaskNextAction(task, next);
            task.ContinueWith(_ => _continuationQueue.Enqueue(taskNext),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, _taskHelper.TaskScheduler);
        }

        private void StopStreamingPull()
        {
            if (_pull != null)
            {
                // Ignore all errors; the stream may be in any state.
                try
                {
                    _registerTaskFn(_pull.WriteCompleteAsync());
                }
                catch { }
                _pull = null;
            }
        }

        // Open streaming-pull, and send initial request to start message stream.
        // If backoff is non-zero delay before opening streaming-pull.
        private void StartStreamingPull()
        {
            if (_pullBackoff is TimeSpan backoff)
            {
                // Delay, then start the streaming-pull.
                Task delayTask = _scheduler.Delay(backoff, _softStopCts.Token);
                Add(delayTask, Next(true, HandleStartStreamingPullWithoutBackoff));
            }
            else
            {
                HandleStartStreamingPullWithoutBackoff();
            }
        }

        // Open streaming-pull, and send initial request to start message stream.
        // Backoff delay (if present) has already been done; no need to delay here.
        private void HandleStartStreamingPullWithoutBackoff()
        {
            _pull = _client.StreamingPull(CallSettings.FromCancellationToken(_softStopCts.Token));
            // Cancellation not needed in this WriteAsync call. The StreamingPull() cancellation
            // (above) will cause this call to cancel if _softStopCts is cancelled.
            Task initTask = _pull.WriteAsync(new StreamingPullRequest
            {
                SubscriptionAsSubscriptionName = _subscriptionName,
                StreamAckDeadlineSeconds = _modifyDeadlineSeconds,
                MaxOutstandingMessages = _useLegacyFlowControl ? 0 : _flow.MaxOutstandingElementCount,
                MaxOutstandingBytes = _useLegacyFlowControl ? 0 : _flow.MaxOutstandingByteCount
            });
            Add(initTask, Next(true, () => HandlePullMoveNext(initTask)));
        }

        private bool HandleRpcFailure(Exception e)
        {
            if (e != null)
            {
                if (e.As<RpcException>()?.IsRecoverable() ?? false)
                {
                    // Recoverable RPC error, stop and restart pull.
                    StopStreamingPull();
                    // Increase backoff internal and start stream again.
                    // If stream-pull fails repeatly, increase the delay, up to a maximum of 30 seconds.
                    _pullBackoff = s_pullBackoff.NextBackoff(_pullBackoff ?? TimeSpan.Zero);
                    StartStreamingPull();
                    return true;
                }
                else
                {
                    // Unrecoverable error; throw it.
                    throw e.FlattenIfPossible();
                }
            }
            return false;
        }

        // Pull-stream is ready; call MoveNext to wait for messages.
        private void HandlePullMoveNext(Task initTask)
        {
            // Check if the init write failed.
            if (initTask != null && HandleRpcFailure(initTask.Exception))
            {
                return;
            }
            // Check if pulls need throttling due to push queues being too full, or too slow to push.
            bool throttle = _extendQueue.TryPeek(out var qItem) && _extendThrottleLow >= qItem.Time;
            if (!throttle)
            {
                int totalQueueCount = _pushInFlight + _extendQueue.Count;
                lock (_lock)
                {
                    totalQueueCount += _ackQueue.Count + _nackQueue.Count;
                }
                throttle = totalQueueCount > _maxAckExtendQueueSize;
            }
            if (throttle)
            {
                // Too many queued ack/nack/extend ids. Loop until the queue has drained a bit.
                Add(_scheduler.Delay(TimeSpan.FromMilliseconds(100), _softStopCts.Token), Next(true, () => HandlePullMoveNext(null)));
            }
            else
            {
                // Call MoveNext to receive more messages.
                // Cancellation is handled by the cancellation-token passed when the stream is created.
                var moveNextTask = _pull.GrpcCall.ResponseStream.MoveNext(CancellationToken.None);
                Add(moveNextTask, Next(true, () => HandlePullMessageData(moveNextTask)));
            }
        }

        // Message-stream has messages (or not, depending on moveNextResult)
        private void HandlePullMessageData(Task<bool> moveNextTask)
        {
            if (HandleRpcFailure(moveNextTask.Exception))
            {
                return;
            }
            if (moveNextTask.Result)
            {
                // Successful receive. Reset pull backoff to zero.
                _pullBackoff = null;
                // Copy msgs to list, and clear original proto repeatedfield; to remove refs to large messages as soon as possible.
                // It is not possible to set RepeatedField elements to null, so messages need transfering to a List.
                StreamingPullResponse current;
                try
                {
                    current = _pull.GrpcCall.ResponseStream.Current;
                    _exactlyOnceDeliveryEnabled = current.SubscriptionProperties?.ExactlyOnceDeliveryEnabled ?? false;
                }
                catch (Exception e) when (e.As<RpcException>()?.IsRecoverable() ?? false)
                {
                    HandleRpcFailure(e);
                    return;
                }
                var receivedMessages = current.ReceivedMessages;
                var msgs = receivedMessages.ToList();
                receivedMessages.Clear();
                // Get all ack-ids, used to extend leases as required.
                var msgIds = new HashSet<string>(msgs.Select(x => x.AckId));
                // Send an initial "lease-extension"; which starts the server timer.
                HandleExtendLease(msgIds, null);
                // Asynchonously start message processing. Handles flow, and calls the user-supplied message handler.
                // Uses Task.Run(), so not to clog up this "master" thread with per-message processing.
                Task messagesTask = _taskHelper.Run(() => ProcessPullMessagesAsync(msgs, msgIds));
                // Once all received messages have been queued for processing, read the stream for more messages.
                Add(messagesTask, Next(true, () => HandlePullMoveNext(null)));
            }
            else
            {
                StopStreamingPull();
                // Always a short pause on server disconnect.
                _pullBackoff = TimeSpan.FromSeconds(0.5);
                StartStreamingPull();
            }
        }

        private async Task ProcessPullMessagesAsync(List<ReceivedMessage> msgs, HashSet<string> msgIds)
        {
            // Running async. Common data needs locking
            for (int msgIndex = 0; msgIndex < msgs.Count; msgIndex++)
            {
                _softStopCts.Token.ThrowIfCancellationRequested();
                var msg = msgs[msgIndex];
                msgs[msgIndex] = null;
                // Prepare to call user message handler, _flow.Process(...) enforces the user-handler concurrency constraints.
                await _taskHelper.ConfigureAwait(_flow.Process(msg.CalculateSize(), msg.Message.OrderingKey ?? "", async () =>
                {
                    // Running async. Common data needs locking
                    lock (_lock)
                    {
                        _softStopCts.Token.ThrowIfCancellationRequested();
                        _userHandlerInFlight += 1;
                    }
                    if (msg.DeliveryAttempt > 0)
                    {
                        msg.Message.Attributes[DeliveryAttemptAttrKey] = msg.DeliveryAttempt.ToString(CultureInfo.InvariantCulture);
                    }
                    // Call user message handler
                    var reply = await _taskHelper.ConfigureAwaitHideErrors(() => _handler.HandleMessage(msg.Message, _hardStopCts.Token), Reply.Nack);
                    // Lock msgsIds, this is accessed concurrently here and in HandleExtendLease().
                    lock (msgIds)
                    {
                        msgIds.Remove(msg.AckId);
                    }
                    // Lock ack/nack-queues, this is accessed concurrently here and in "master" thread.
                    lock (_lock)
                    {
                        _userHandlerInFlight -= 1;
                        var queue = reply == Reply.Ack ? _ackQueue : _nackQueue;
                        queue.Enqueue(msg.AckId);
                    }
                    // Ids have been added to ack/nack-queue, so trigger a push.
                    _eventPush.Set();
                }));
            }
        }

        private class LeaseCancellation
        {
            private readonly object _lock = new object();
            private CancellationTokenSource _cts;

            public CancellationToken Token
            {
                get
                {
                    lock (_lock)
                    {
                        return _cts?.Token ?? CancellationToken.None;
                    }
                }
            }

            public LeaseCancellation(CancellationTokenSource softStopCts) =>
                _cts = CancellationTokenSource.CreateLinkedTokenSource(softStopCts.Token);
           
            public void Dispose()
            {
                lock (_lock)
                {
                    _cts.Dispose();
                    _cts = null;
                }
            }

            public bool IsDisposed
            {
                get
                {
                    lock (_lock)
                    {
                        return _cts == null;
                    }
                }
            }

            public void Cancel()
            {
                CancellationTokenSource cts2;
                lock (_lock)
                {
                    cts2 = _cts;
                }
                // Cancel outside of lock, as continuations may be executed synchronously.
                cts2?.Cancel();
                // No need to dispose of `_cts` here, as `Dispose()` will always be called.
            }
        }

        private void HandleExtendLease(HashSet<string> msgIds, LeaseCancellation cancellation)
        {
            if (_softStopCts.IsCancellationRequested)
            {
                // No further lease extensions once stop is requested.
                return;
            }
            // The first call to this method happens as soon as messages in this chunk start to be processed.
            // This triggers the server to start its lease timer.
            if (cancellation == null)
            {
                // Create a task to cancel lease-extension once `_maxExtensionDuration` has been reached.
                // This set up once for each chunk of received messages, and passed through to each future call to this method.
                cancellation = new LeaseCancellation(_softStopCts);
                Add(_scheduler.Delay(_maxExtensionDuration, cancellation.Token), Next(false, () =>
                {
                    // This is executed when `_maxExtensionDuration` has expired, or when `cancellation` is cancelled,
                    // Which ensures `cancellation` is aways disposed of.
                    cancellation.Dispose();
                    lock (msgIds)
                    {
                        msgIds.Clear();
                    }
                }));
            }
            if (!cancellation.IsDisposed)
            {
                // If `_maxExtensionDuration` has not expired, then schedule a further lease extension.
                bool anyMsgIds;
                lock (msgIds)
                {
                    anyMsgIds = msgIds.Count > 0;
                    if (anyMsgIds)
                    {
                        lock (_lock)
                        {
                            _extendQueue.Enqueue(msgIds.Select(x => new TimedId(_extendThrottleHigh + 1, x)));
                        }
                    }
                }
                if (anyMsgIds)
                {
                    // Ids have been added to _extendQueue, so trigger a push.
                    _eventPush.Set();
                    // Some ids still exist, schedule another extension.
                    // The overall `_maxExtensionDuration` is maintained by passing through the existing `cancellation`.
                    Add(_scheduler.Delay(_exactlyOnceDeliveryEnabled ? _autoExtendIntervalForExactlyOnceDelivery : _autoExtendInterval, _softStopCts.Token), Next(false, () => HandleExtendLease(msgIds, cancellation)));
                    // Increment _extendThrottles.
                    _extendThrottleHigh += 1;
                    Add(_scheduler.Delay(_extendQueueThrottleInterval, _softStopCts.Token), Next(false, () => _extendThrottleLow += 1));
                }
                else
                {
                    // All messages have been handled in this chunk, so cancel the max-lease-time monitoring.
                    // This will also cause `cancellation` to be disposed in the anonymous function above.
                    cancellation.Cancel();
                }
            }
        }

        private void HandlePush()
        {
            // Always re-listen for push events.
            Add(_eventPush.WaitAsync(_pushStopCts.Token), Next(false, HandlePush));
            // Send data, if there is any to send.
            StartPush();
        }

        private void StartPush()
        {
            // Send data, if there is any to send.
            if (_concurrentPushCount >= _maxConcurrentPush)
            {
                // Too many existing concurrent pushes; do nothing.
                return;
            }
            List<string> acks;
            List<TimedId> extends;
            List<string> nacks;
            lock (_lock)
            {
                // Priority of sending: Acks, Extends, Nacks.
                // This code can cause the number of concurrent pushes to go over _maxConcurrentPush
                // by 2. Allow this, to ensure all queues get fairly sent.
                acks = _ackQueue.Dequeue(_maxAckExtendSendCount, null);
                nacks = _nackQueue.Dequeue(_maxAckExtendSendCount, null);
                var ackSet = new HashSet<string>(acks);
                var nackSet = new HashSet<string>(nacks);
                // Only send extends for ids that aren't also about to ack or nack.
                extends = _extendQueue.Dequeue(_maxAckExtendSendCount, x => !ackSet.Contains(x.Id) && !nackSet.Contains(x.Id));
            }
            if (acks.Count > 0)
            {
                _pushInFlight += acks.Count;
                _concurrentPushCount += 1;
                Task ackTask = _client.AcknowledgeAsync(_subscriptionName, acks, _hardStopCts.Token);
                Add(ackTask, Next(false, () => HandleAckResponse(ackTask, acks, null, null)));
            }
            if (extends.Count > 0)
            {
                _pushInFlight += extends.Count;
                _concurrentPushCount += 1;
                Task extendTask = _client.ModifyAckDeadlineAsync(_subscriptionName, extends.Select(x => x.Id), _modifyDeadlineSeconds, _hardStopCts.Token);
                Add(extendTask, Next(false, () => HandleAckResponse(extendTask, null, null, extends)));
            }
            if (nacks.Count > 0)
            {
                _pushInFlight += nacks.Count;
                _concurrentPushCount += 1;
                Task nackTask = _client.ModifyAckDeadlineAsync(_subscriptionName, nacks, 0, _hardStopCts.Token);
                Add(nackTask, Next(false, () => HandleAckResponse(nackTask, null, nacks, null)));
            }
        }

        /// <summary>
        /// This method is called when the acknowledgement request completes.
        /// This method handles the response from an acknowledgement request for exactly once delivery subscriptions only.
        /// For non-exactly once delivery subscriptions, <see cref="HandleAckResponse(Task, List{string}, List{string}, List{TimedId})"/> is called.
        /// </summary>
        /// <param name="writeTask">The task containing the response of Acknowledge/ModifyAckDeadline RPC.</param>
        /// <param name="ackIds">The list of ids that were sent to the Acknowledge RPC for acknowledgement
        /// that the message associated with these ids shouldn't be redelivered.</param>
        /// <param name="nackIds">The list of ids that were sent to the ModifyAckDeadline RPC to indicate that
        /// the message associated with these ids should be redelivered.</param>
        /// <param name="extendIds">The list of ids that were sent to the ModifyAckDeadline RPC to modify their ack deadline.</param>
        /// <remarks>
        /// Only one of ackIds, nackIds and extendIds will be non-null and based on that we determine if we are handling acks, nacks or extends.
        /// This method is used for exactly once delivery subscriptions only. To keep the logic of exactly once delivery subscription in one place,
        /// this method has a few local methods that are used only in exactly once delivery flow and not in non-exactly once delivery flow, so the method
        /// is pretty long.
        /// </remarks>
        private void HandleAckResponseForExactlyOnceDelivery(Task writeTask, List<string> ackIds, List<string> nackIds, List<TimedId> extendIds)
        {
            _concurrentPushCount -= 1;
            _pushInFlight -= (ackIds?.Count ?? 0) + (nackIds?.Count ?? 0) + (extendIds?.Count ?? 0);
            
            bool hasAckIds = ackIds?.Count > 0;
            bool hasNackIds = nackIds?.Count > 0;
            bool hasExtendIds = extendIds?.Count > 0;
            // In one call, we'll have one of acks or nacks or extends, not all.
            GaxPreconditions.CheckArgument((hasAckIds ? 1 : 0) + (hasNackIds ? 1 : 0) + (hasExtendIds ? 1 : 0) <= 1, nameof(ackIds), "At most one of ackIds, nackIds or extendIds should be non-empty.");
            var ids = hasAckIds ? ackIds
                : hasNackIds ? nackIds : extendIds.Select(j => j.Id);

            if (writeTask.IsFaulted)
            {
                // There may be few successes and few failures or all failures.
                var rpcException = writeTask.Exception.As<RpcException>();
                if (rpcException != null)
                {
                    var ackError = AckError.ForRpcException(rpcException, ids);
                    // Only temporary failures should be retried. Mark them as retryable if not already.
                    UpdateRetryableIds(ids, ackError);

                    if (hasAckIds)
                    {
                        RetryAcks(ackError);
                    }

                    if (hasNackIds)
                    {
                        RetryNacks(ackError);
                    }

                    if (hasExtendIds)
                    {
                        RetryExtends(extendIds, ackError);
                    }
                }
                else
                {
                    // Throw it.
                    throw writeTask.Exception.FlattenIfPossible();
                }
            }
            else
            {
                // Everything succeeded. Update _retryableIds dictionary.
                UpdateRetryableIds(ids, default);
            }

            // Perform push so that other messages can be processed.
            StartPush();
            return;

            // All methods below are local methods applicable to exactly once delivery subscriptions only.
            // They are not used in non-exactly once delivery subscriptions.

            // This local method used for exactly once delivery only, schedules the retry of ackIds with temporary errors.
            void RetryAcks(AckError ackError) =>
                // ack = true implies acks.
                RetryAcksAndNacks(true, ackError);

            // This local method used for exactly once delivery only, schedules the retry of nackIds with temporary errors.
            void RetryNacks(AckError ackError) =>
                // ack = false implies nacks.
                RetryAcksAndNacks(false, ackError);

            // This local method used for exactly once delivery only, schedules the retry of ackIds/nackIds with temporary errors.
            void RetryAcksAndNacks(bool ack, AckError ackError)
            {
                // ack = true implies acks. ack = false implies nacks.
                // We have acks or nacks.
                var idsToRetry = ackError.TemporaryFailureIds;
                var queue = ack ? _ackQueue : _nackQueue;
                if (idsToRetry.Any())
                {
                    // Retry for 600 seconds.
                    RetryTemporaryFailures(idsToRetry, acksToRetry => queue.Locked(() => queue.Requeue(acksToRetry)), Enumerable.Empty<TimedId>(), null, 600);
                }
            }

            // This local method used for exactly once delivery only, schedules the retry of extendIds with temporary errors.
            void RetryExtends(IEnumerable<TimedId> extendIds, AckError ackError)
            {
                var extendIdsToRetry = extendIds.Where(j => ackError.TemporaryFailureIds.Contains(j.Id));
                if (extendIdsToRetry.Any())
                {
                    // Retry for 10 seconds or 3 attempts only.
                    RetryTemporaryFailures(Enumerable.Empty<string>(), null, extendIdsToRetry, extendsToRetry => _extendQueue.Locked(() => _extendQueue.Requeue(extendsToRetry)), 10);
                }
            }

            // This local method used for exactly once delivery only, retries the ackIds/nackIds with temporary failures for Acknowledge RPC or extendIds with temporary failures for ModifyAckDeadline RPC.
            // This method checks if the id exists in _retryableIds dictionary and has not been tried, or not been successful for the specified timeout since the first time of failure.
            // If so, it retries those ids by adding them to the appropriate queue and calls the StartPush() method.
            // For acks and nacks, ackOrNackIds must be non-null. For extends, extendIds must be non-null.
            // For acks and nacks, ackActionToRetry must be non-null and specify the right queue (_ackQueue or _nackQueue).
            // For extends, extendActionToRetry must be non-null and specify the right queue,i.e, _extendQueue.
            void RetryTemporaryFailures(
                IEnumerable<string> ackOrNackIds,
                Action<IEnumerable<string>> ackActionToRetry,
                IEnumerable<TimedId> extendIds,
                Action<IEnumerable<TimedId>> extendActionToRetry = default,
                int timeoutInSeconds = 600)
            {
                // ackOrNackIds may be empty but will never be null, so null check is not needed.
                // By design, only items in ackOrNackIds that exist in _retryableIds will be retried.
                // If id is not in _retryableIds dictionary, it is either already succeeded or not retryable.
                // Failure may be non-retryable due to exceeded time duration or that failure may have become permanent.

                bool hasAcksOrNacks = ackOrNackIds.Any();
                bool hasExtends = extendIds.Any();
                if (!hasAcksOrNacks && !hasExtends)
                {
                    return;
                }

                var idsToRetry = hasAcksOrNacks ? ackOrNackIds : extendIds.Select(j => j.Id);
                var retriesToSchedule = new List<(string id, RetryInfo info)>();

                foreach (var candidateRetryId in idsToRetry)
                {
                    if (!_retryableIds.TryGetValue(candidateRetryId, out var retryInfo))
                    {
                        continue;
                    }
                    var backoff = s_ackBackoff.NextBackoff(retryInfo.Backoff ?? TimeSpan.Zero);
                    // We should retry only for specified timeout.
                    if (_clock.GetCurrentDateTimeUtc() > retryInfo.FirstTimeOfFailureInUtc + TimeSpan.FromSeconds(timeoutInSeconds) - backoff)
                    {
                        // We are past the retry timeout. This id is not retryable.
                        // Remove this id from _retryableIds dictionary and continue.
                        _ = _retryableIds.TryRemove(candidateRetryId, out _);
                        continue;
                    }

                    retryInfo = retryInfo.WithBackoff(backoff);

                    // Update the backoff for this ID in the _retryableIds dictionary.
                    _retryableIds[candidateRetryId] = retryInfo;
                    // Add it to the list of retries we need to attempt after grouping.
                    retriesToSchedule.Add((candidateRetryId, retryInfo));
                }

                // We can have ids with different backoff values.
                // Group on the backoff and add them to the corresponding queue in the increasing order of backoff.
                var retryGroups = retriesToSchedule.GroupBy(info => info.info.Backoff).OrderBy(group => group.Key);

                foreach (var retryGroup in retryGroups)
                {
                    var backoff = retryGroup.Key ?? TimeSpan.Zero;
                    var retryIds = retryGroup.Select(j => j.id);
                    Task delayTask = _scheduler.Delay(backoff, _softStopCts.Token);
                    Add(delayTask, new NextAction(false, hasAcksOrNacks
                        ? () => { ackActionToRetry(retryIds); StartPush(); }
                        : () => { extendActionToRetry(extendIds.Where(j => retryIds.Contains(j.Id))); StartPush(); }));
                }
            }

            // This local method used for exactly once delivery only, updates the _retryableIds dictionary. Ensures that the ids that should be re-tried exist in the dictionary.
            // By design, only ids that are in _retryableIds dictionary will be retried with exponential backoff.
            // Removes the ids that are either successful or become permanent failures as they should not be retried.
            // allIds - All the ids for which Acknowledge/ModifyAckDeadline request was sent.
            // ackError - The AckError that occurred in the response.
            void UpdateRetryableIds(IEnumerable<string> allIds, AckError ackError)
            {
                var temporaryFailureIds = ackError?.TemporaryFailureIds ?? Enumerable.Empty<string>();
                var permanentFailureIds = ackError?.PermanentFailureIds ?? Enumerable.Empty<string>();
                // Successful Ids = AllIds - (TemporaryFailures + Permanent failures)
                // TODO: Check if there is an impact due to lazy loading of IEnumerable<T>.
                var successfulIds = allIds.Except(temporaryFailureIds).Except(permanentFailureIds);
                // Some ids may have permanent failures and some may have succeeded. Those ids shouldn't be retried. 
                var nonRetryableIds = permanentFailureIds.Concat(successfulIds);

                foreach (var item in temporaryFailureIds)
                {
                    // If the id to be retried is already in _retryableIds dictionary we are good,
                    // else add the id to _retryableIds dictionary with first time of failure as CurrentDateTimeUtc,
                    // so that we can retry for specific duration.
                    _ = _retryableIds.TryAdd(item, new RetryInfo(_clock.GetCurrentDateTimeUtc()));
                }

                // Remove non-retryable ids from _retryableIds dictionary.
                foreach (var item in nonRetryableIds)
                {
                    _ = _retryableIds.TryRemove(item, out _);
                }
            }
        }

        private void HandleAckResponse(Task writeTask, List<string> ackIds, List<string> nackIds, List<TimedId> extendIds)
        {
            bool hasAckIds = ackIds?.Count > 0;
            bool hasNackIds = nackIds?.Count > 0;
            var ids = hasAckIds ? ackIds
                : hasNackIds ? nackIds
                : new List<string>();
            // Subscription users can only request to ACK/NACK the message,
            // so fetch the status of ACK-ed/NACK-ed message only (not extends).
            // The handlers will be invoked only when ACK/NACK response is being processed.
            var ackNackResponses = ids.Count > 0
                ? GetAckNackResponses(ids, writeTask)
                : new List<AckNackResponse>();
            if (hasAckIds)
            {
                // Invoke the handler for ACKs.
                // Any uncaught exception in the handler will terminate the client.
                _handler.HandleAckResponses(ackNackResponses);
            }
            else if (hasNackIds)
            {
                // Invoke the handler for NACKs.
                // Any uncaught exception in the handler will terminate the client.
                _handler.HandleNackResponses(ackNackResponses);
            }
            
            if (_exactlyOnceDeliveryEnabled)
            {
                HandleAckResponseForExactlyOnceDelivery(writeTask, ackIds, nackIds, extendIds);
                return;
            }

            _concurrentPushCount -= 1;
            _pushInFlight -= (ackIds?.Count ?? 0) + (nackIds?.Count ?? 0) + (extendIds?.Count ?? 0);
            if (writeTask.IsFaulted)
            {
                // Check if it's an RpcException. If it is, then ignore it and continue. We may want to log it later. 
                // Other non-gRPC unrecoverable errors will continue to be thrown.
                if (writeTask.Exception.As<RpcException>() is null)
                {
                    // It is a non-gRPC unrecoverable error; throw exception.
                    throw writeTask.Exception.FlattenIfPossible();
                }
            }
            // Immediately send more data if there is any to send.
            StartPush();
            return;

            // This local method gets the status of ACK/NACK response for the successful and permanently failed ids as the sequence of AckNackResponse.
            // ids - The list of all the ids being ACK-ed/NACK-ed for which the AckNackResponse is required.
            // writeTask - The task that contains the result of ACK/NACK request for the given ids.
            // Returns the status of ACK/NACK response for the successful and permanently failed ids as the sequence of AckNackResponse.
            IReadOnlyList<AckNackResponse> GetAckNackResponses(List<string> ids, Task writeTask)
            {
                // Either the task would have succeeded or faulted. Let's check for faults.
                if (writeTask.IsFaulted)
                {
                    // There may be few successes and few failures or all failures.
                    var rpcException = writeTask.Exception.As<RpcException>();
                    if (rpcException != null && _exactlyOnceDeliveryEnabled)
                    {
                        // Exactly once delivery scenario.
                        // Populate AckNackResponse with success and permanent failed ids only.
                        // Temporary errors are deliberately ignored for maintaining consistency with the other client libraries.
                        // Temporary errors will eventually become successful or fail permanently. Their status would be shared then.
                        var ackError = AckError.ForRpcException(rpcException, ids);
                        return ackError.GetAckNackResponses().ToList();
                    }
                    else if (rpcException != null)
                    {
                        // Non-exactly once delivery scenario with RpcException.
                        // All RpcExceptions are either recoverable or ignored. Non-RpcException would throw.
                        // As a requirement from Pub/Sub team, all RpcExceptions in non-exactly once delivery scenario should be treated as fire and forget,
                        // so we always treat them as successful (even when they are not). If there are recoverable errors, the ACK/NACK operation
                        // will be retried, so the message may be sent multiple times with success status.
                        return GetSuccessResponses(ids);
                    }
                    else
                    {
                        // Non-gRPC error will throw exception eventually, so don't return those IDs as success. Return an empty list.
                        return new List<AckNackResponse>();
                    }
                }

                // If we are here, it means every ID succeeded in the request.
                // Create successful AckNackResponse for every ID and return the list.
                return GetSuccessResponses(ids);

                // This method is local to GetAckNackResponses and gets the successful AckNackResponse for every given ID.
                // It takes the list of IDs that are deemed to be successfully ACK-ed or NACK-ed.
                // The corresponding AckNackResponse with success status is created for every ID and returned.
                static IReadOnlyList<AckNackResponse> GetSuccessResponses(List<string> successfulIds) =>
                    successfulIds.ConvertAll(item => new AckNackResponse(item, AcknowledgementStatus.Success, default));
            }
        }

        private void HandleStreamPing()
        {
            // Need to explicitly check this, as the continuation passed to Add() may be executed
            // regardless of the fault/cancellation state of the Task.
            if (_softStopCts.IsCancellationRequested)
            {
                // No more pings when subscriber stopping.
                return;
            }
            // Schedule next ping, this never stops whilst this subscriber as active
            Add(_scheduler.Delay(s_streamPingPeriod, _softStopCts.Token), Next(false, HandleStreamPing));
            // If messages are currently being processed, then ping the stream periodically;
            // this ensures the stream isn't closed.
            // If the stream is closed, then all gRPC-buffered messages have their server-side
            // expiry timers started, when the client hasn't yet started processing these messages.
            // This can lead to unncessarily duplicated messages.
            if (!IsPushComplete() && _pull != null)
            {
                // Write an empty message to the stream.
                // Don't wait for this to complete, it doesn't matter if or when it completes.
                // This is only a best-effort attempt to reduce duplicate messages.
                try
                {
                    _pull.WriteAsync(new StreamingPullRequest());
                }
                catch
                {
                    // Ignore any errors.
                }
            }
        }
    }
}
