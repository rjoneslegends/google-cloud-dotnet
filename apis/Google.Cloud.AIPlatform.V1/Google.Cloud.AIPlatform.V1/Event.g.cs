// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: google/cloud/aiplatform/v1/event.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Google.Cloud.AIPlatform.V1 {

  /// <summary>Holder for reflection information generated from google/cloud/aiplatform/v1/event.proto</summary>
  public static partial class EventReflection {

    #region Descriptor
    /// <summary>File descriptor for google/cloud/aiplatform/v1/event.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static EventReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiZnb29nbGUvY2xvdWQvYWlwbGF0Zm9ybS92MS9ldmVudC5wcm90bxIaZ29v",
            "Z2xlLmNsb3VkLmFpcGxhdGZvcm0udjEaH2dvb2dsZS9hcGkvZmllbGRfYmVo",
            "YXZpb3IucHJvdG8aGWdvb2dsZS9hcGkvcmVzb3VyY2UucHJvdG8aH2dvb2ds",
            "ZS9wcm90b2J1Zi90aW1lc3RhbXAucHJvdG8imAMKBUV2ZW50EjwKCGFydGlm",
            "YWN0GAEgASgJQirgQQL6QSQKImFpcGxhdGZvcm0uZ29vZ2xlYXBpcy5jb20v",
            "QXJ0aWZhY3QSPgoJZXhlY3V0aW9uGAIgASgJQivgQQP6QSUKI2FpcGxhdGZv",
            "cm0uZ29vZ2xlYXBpcy5jb20vRXhlY3V0aW9uEjMKCmV2ZW50X3RpbWUYAyAB",
            "KAsyGi5nb29nbGUucHJvdG9idWYuVGltZXN0YW1wQgPgQQMSOQoEdHlwZRgE",
            "IAEoDjImLmdvb2dsZS5jbG91ZC5haXBsYXRmb3JtLnYxLkV2ZW50LlR5cGVC",
            "A+BBAhI9CgZsYWJlbHMYBSADKAsyLS5nb29nbGUuY2xvdWQuYWlwbGF0Zm9y",
            "bS52MS5FdmVudC5MYWJlbHNFbnRyeRotCgtMYWJlbHNFbnRyeRILCgNrZXkY",
            "ASABKAkSDQoFdmFsdWUYAiABKAk6AjgBIjMKBFR5cGUSFAoQVFlQRV9VTlNQ",
            "RUNJRklFRBAAEgkKBUlOUFVUEAESCgoGT1VUUFVUEAJCyAEKHmNvbS5nb29n",
            "bGUuY2xvdWQuYWlwbGF0Zm9ybS52MUIKRXZlbnRQcm90b1ABWj5jbG91ZC5n",
            "b29nbGUuY29tL2dvL2FpcGxhdGZvcm0vYXBpdjEvYWlwbGF0Zm9ybXBiO2Fp",
            "cGxhdGZvcm1wYqoCGkdvb2dsZS5DbG91ZC5BSVBsYXRmb3JtLlYxygIaR29v",
            "Z2xlXENsb3VkXEFJUGxhdGZvcm1cVjHqAh1Hb29nbGU6OkNsb3VkOjpBSVBs",
            "YXRmb3JtOjpWMWIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Api.FieldBehaviorReflection.Descriptor, global::Google.Api.ResourceReflection.Descriptor, global::Google.Protobuf.WellKnownTypes.TimestampReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Google.Cloud.AIPlatform.V1.Event), global::Google.Cloud.AIPlatform.V1.Event.Parser, new[]{ "Artifact", "Execution", "EventTime", "Type", "Labels" }, null, new[]{ typeof(global::Google.Cloud.AIPlatform.V1.Event.Types.Type) }, null, new pbr::GeneratedClrTypeInfo[] { null, })
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// An edge describing the relationship between an Artifact and an Execution in
  /// a lineage graph.
  /// </summary>
  public sealed partial class Event : pb::IMessage<Event>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<Event> _parser = new pb::MessageParser<Event>(() => new Event());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<Event> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Google.Cloud.AIPlatform.V1.EventReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Event() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Event(Event other) : this() {
      artifact_ = other.artifact_;
      execution_ = other.execution_;
      eventTime_ = other.eventTime_ != null ? other.eventTime_.Clone() : null;
      type_ = other.type_;
      labels_ = other.labels_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Event Clone() {
      return new Event(this);
    }

    /// <summary>Field number for the "artifact" field.</summary>
    public const int ArtifactFieldNumber = 1;
    private string artifact_ = "";
    /// <summary>
    /// Required. The relative resource name of the Artifact in the Event.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Artifact {
      get { return artifact_; }
      set {
        artifact_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "execution" field.</summary>
    public const int ExecutionFieldNumber = 2;
    private string execution_ = "";
    /// <summary>
    /// Output only. The relative resource name of the Execution in the Event.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Execution {
      get { return execution_; }
      set {
        execution_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "event_time" field.</summary>
    public const int EventTimeFieldNumber = 3;
    private global::Google.Protobuf.WellKnownTypes.Timestamp eventTime_;
    /// <summary>
    /// Output only. Time the Event occurred.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Google.Protobuf.WellKnownTypes.Timestamp EventTime {
      get { return eventTime_; }
      set {
        eventTime_ = value;
      }
    }

    /// <summary>Field number for the "type" field.</summary>
    public const int TypeFieldNumber = 4;
    private global::Google.Cloud.AIPlatform.V1.Event.Types.Type type_ = global::Google.Cloud.AIPlatform.V1.Event.Types.Type.Unspecified;
    /// <summary>
    /// Required. The type of the Event.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Google.Cloud.AIPlatform.V1.Event.Types.Type Type {
      get { return type_; }
      set {
        type_ = value;
      }
    }

    /// <summary>Field number for the "labels" field.</summary>
    public const int LabelsFieldNumber = 5;
    private static readonly pbc::MapField<string, string>.Codec _map_labels_codec
        = new pbc::MapField<string, string>.Codec(pb::FieldCodec.ForString(10, ""), pb::FieldCodec.ForString(18, ""), 42);
    private readonly pbc::MapField<string, string> labels_ = new pbc::MapField<string, string>();
    /// <summary>
    /// The labels with user-defined metadata to annotate Events.
    ///
    /// Label keys and values can be no longer than 64 characters
    /// (Unicode codepoints), can only contain lowercase letters, numeric
    /// characters, underscores and dashes. International characters are allowed.
    /// No more than 64 user labels can be associated with one Event (System
    /// labels are excluded).
    ///
    /// See https://goo.gl/xmQnxf for more information and examples of labels.
    /// System reserved label keys are prefixed with "aiplatform.googleapis.com/"
    /// and are immutable.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::MapField<string, string> Labels {
      get { return labels_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as Event);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(Event other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Artifact != other.Artifact) return false;
      if (Execution != other.Execution) return false;
      if (!object.Equals(EventTime, other.EventTime)) return false;
      if (Type != other.Type) return false;
      if (!Labels.Equals(other.Labels)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (Artifact.Length != 0) hash ^= Artifact.GetHashCode();
      if (Execution.Length != 0) hash ^= Execution.GetHashCode();
      if (eventTime_ != null) hash ^= EventTime.GetHashCode();
      if (Type != global::Google.Cloud.AIPlatform.V1.Event.Types.Type.Unspecified) hash ^= Type.GetHashCode();
      hash ^= Labels.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (Artifact.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Artifact);
      }
      if (Execution.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Execution);
      }
      if (eventTime_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(EventTime);
      }
      if (Type != global::Google.Cloud.AIPlatform.V1.Event.Types.Type.Unspecified) {
        output.WriteRawTag(32);
        output.WriteEnum((int) Type);
      }
      labels_.WriteTo(output, _map_labels_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (Artifact.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Artifact);
      }
      if (Execution.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Execution);
      }
      if (eventTime_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(EventTime);
      }
      if (Type != global::Google.Cloud.AIPlatform.V1.Event.Types.Type.Unspecified) {
        output.WriteRawTag(32);
        output.WriteEnum((int) Type);
      }
      labels_.WriteTo(ref output, _map_labels_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (Artifact.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Artifact);
      }
      if (Execution.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Execution);
      }
      if (eventTime_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(EventTime);
      }
      if (Type != global::Google.Cloud.AIPlatform.V1.Event.Types.Type.Unspecified) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) Type);
      }
      size += labels_.CalculateSize(_map_labels_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(Event other) {
      if (other == null) {
        return;
      }
      if (other.Artifact.Length != 0) {
        Artifact = other.Artifact;
      }
      if (other.Execution.Length != 0) {
        Execution = other.Execution;
      }
      if (other.eventTime_ != null) {
        if (eventTime_ == null) {
          EventTime = new global::Google.Protobuf.WellKnownTypes.Timestamp();
        }
        EventTime.MergeFrom(other.EventTime);
      }
      if (other.Type != global::Google.Cloud.AIPlatform.V1.Event.Types.Type.Unspecified) {
        Type = other.Type;
      }
      labels_.Add(other.labels_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Artifact = input.ReadString();
            break;
          }
          case 18: {
            Execution = input.ReadString();
            break;
          }
          case 26: {
            if (eventTime_ == null) {
              EventTime = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            }
            input.ReadMessage(EventTime);
            break;
          }
          case 32: {
            Type = (global::Google.Cloud.AIPlatform.V1.Event.Types.Type) input.ReadEnum();
            break;
          }
          case 42: {
            labels_.AddEntriesFrom(input, _map_labels_codec);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            Artifact = input.ReadString();
            break;
          }
          case 18: {
            Execution = input.ReadString();
            break;
          }
          case 26: {
            if (eventTime_ == null) {
              EventTime = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            }
            input.ReadMessage(EventTime);
            break;
          }
          case 32: {
            Type = (global::Google.Cloud.AIPlatform.V1.Event.Types.Type) input.ReadEnum();
            break;
          }
          case 42: {
            labels_.AddEntriesFrom(ref input, _map_labels_codec);
            break;
          }
        }
      }
    }
    #endif

    #region Nested types
    /// <summary>Container for nested types declared in the Event message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static partial class Types {
      /// <summary>
      /// Describes whether an Event's Artifact is the Execution's input or output.
      /// </summary>
      public enum Type {
        /// <summary>
        /// Unspecified whether input or output of the Execution.
        /// </summary>
        [pbr::OriginalName("TYPE_UNSPECIFIED")] Unspecified = 0,
        /// <summary>
        /// An input of the Execution.
        /// </summary>
        [pbr::OriginalName("INPUT")] Input = 1,
        /// <summary>
        /// An output of the Execution.
        /// </summary>
        [pbr::OriginalName("OUTPUT")] Output = 2,
      }

    }
    #endregion

  }

  #endregion

}

#endregion Designer generated code
