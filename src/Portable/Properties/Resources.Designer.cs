﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hermes.Properties {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Hermes.Properties.Resources", typeof(Resources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bit position must be between 0 and 7.
        /// </summary>
        internal static string ByteExtensions_InvalidBitPosition {
            get {
                return ResourceManager.GetString("ByteExtensions_InvalidBitPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only numbers and letters are allowed for Client Id value.
        /// </summary>
        internal static string ConnectFormatter_ClientIdInvalid {
            get {
                return ResourceManager.GetString("ConnectFormatter_ClientIdInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client Id cannot exceed 23 bytes.
        /// </summary>
        internal static string ConnectFormatter_ClientIdMaxLengthExceeded {
            get {
                return ResourceManager.GetString("ConnectFormatter_ClientIdMaxLengthExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client Id value is cannot be null or empty.
        /// </summary>
        internal static string ConnectFormatter_ClientIdRequired {
            get {
                return ResourceManager.GetString("ConnectFormatter_ClientIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to String value cannot exceed 65536 bytes of length.
        /// </summary>
        internal static string DataRepresentationExtensions_StringMaxLengthExceeded {
            get {
                return ResourceManager.GetString("DataRepresentationExtensions_StringMaxLengthExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} message is not valid.
        /// </summary>
        internal static string Formatter_MessageTypeNotAllowed {
            get {
                return ResourceManager.GetString("Formatter_MessageTypeNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Malformed Remaining Length.
        /// </summary>
        internal static string ProtocolEncoding_MalformedRemainingLength {
            get {
                return ResourceManager.GetString("ProtocolEncoding_MalformedRemainingLength", resourceCulture);
            }
        }
    }
}
