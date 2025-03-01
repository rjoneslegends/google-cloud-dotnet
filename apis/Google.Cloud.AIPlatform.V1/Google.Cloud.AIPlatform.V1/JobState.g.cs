// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: google/cloud/aiplatform/v1/job_state.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Google.Cloud.AIPlatform.V1 {

  /// <summary>Holder for reflection information generated from google/cloud/aiplatform/v1/job_state.proto</summary>
  public static partial class JobStateReflection {

    #region Descriptor
    /// <summary>File descriptor for google/cloud/aiplatform/v1/job_state.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static JobStateReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cipnb29nbGUvY2xvdWQvYWlwbGF0Zm9ybS92MS9qb2Jfc3RhdGUucHJvdG8S",
            "Gmdvb2dsZS5jbG91ZC5haXBsYXRmb3JtLnYxKpACCghKb2JTdGF0ZRIZChVK",
            "T0JfU1RBVEVfVU5TUEVDSUZJRUQQABIUChBKT0JfU1RBVEVfUVVFVUVEEAES",
            "FQoRSk9CX1NUQVRFX1BFTkRJTkcQAhIVChFKT0JfU1RBVEVfUlVOTklORxAD",
            "EhcKE0pPQl9TVEFURV9TVUNDRUVERUQQBBIUChBKT0JfU1RBVEVfRkFJTEVE",
            "EAUSGAoUSk9CX1NUQVRFX0NBTkNFTExJTkcQBhIXChNKT0JfU1RBVEVfQ0FO",
            "Q0VMTEVEEAcSFAoQSk9CX1NUQVRFX1BBVVNFRBAIEhUKEUpPQl9TVEFURV9F",
            "WFBJUkVEEAkSFgoSSk9CX1NUQVRFX1VQREFUSU5HEApCywEKHmNvbS5nb29n",
            "bGUuY2xvdWQuYWlwbGF0Zm9ybS52MUINSm9iU3RhdGVQcm90b1ABWj5jbG91",
            "ZC5nb29nbGUuY29tL2dvL2FpcGxhdGZvcm0vYXBpdjEvYWlwbGF0Zm9ybXBi",
            "O2FpcGxhdGZvcm1wYqoCGkdvb2dsZS5DbG91ZC5BSVBsYXRmb3JtLlYxygIa",
            "R29vZ2xlXENsb3VkXEFJUGxhdGZvcm1cVjHqAh1Hb29nbGU6OkNsb3VkOjpB",
            "SVBsYXRmb3JtOjpWMWIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Google.Cloud.AIPlatform.V1.JobState), }, null, null));
    }
    #endregion

  }
  #region Enums
  /// <summary>
  /// Describes the state of a job.
  /// </summary>
  public enum JobState {
    /// <summary>
    /// The job state is unspecified.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_UNSPECIFIED")] Unspecified = 0,
    /// <summary>
    /// The job has been just created or resumed and processing has not yet begun.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_QUEUED")] Queued = 1,
    /// <summary>
    /// The service is preparing to run the job.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_PENDING")] Pending = 2,
    /// <summary>
    /// The job is in progress.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_RUNNING")] Running = 3,
    /// <summary>
    /// The job completed successfully.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_SUCCEEDED")] Succeeded = 4,
    /// <summary>
    /// The job failed.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_FAILED")] Failed = 5,
    /// <summary>
    /// The job is being cancelled. From this state the job may only go to
    /// either `JOB_STATE_SUCCEEDED`, `JOB_STATE_FAILED` or `JOB_STATE_CANCELLED`.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_CANCELLING")] Cancelling = 6,
    /// <summary>
    /// The job has been cancelled.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_CANCELLED")] Cancelled = 7,
    /// <summary>
    /// The job has been stopped, and can be resumed.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_PAUSED")] Paused = 8,
    /// <summary>
    /// The job has expired.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_EXPIRED")] Expired = 9,
    /// <summary>
    /// The job is being updated. The job is only able to be updated at RUNNING
    /// state; if the update operation succeeds, job goes back to RUNNING state; if
    /// the update operation fails, the job goes back to RUNNING state with error
    /// messages written to [ModelDeploymentMonitoringJob.partial_errors][] field
    /// if it is a ModelDeploymentMonitoringJob.
    /// </summary>
    [pbr::OriginalName("JOB_STATE_UPDATING")] Updating = 10,
  }

  #endregion

}

#endregion Designer generated code
