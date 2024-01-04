﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LCVR.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LCVR.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to {
        ///  &quot;name&quot;: &quot;PlayerActions&quot;,
        ///  &quot;maps&quot;: [
        ///    {
        ///      &quot;name&quot;: &quot;Movement&quot;,
        ///      &quot;id&quot;: &quot;1560e87b-23aa-4005-bf8b-264f6a3c3736&quot;,
        ///      &quot;actions&quot;: [
        ///        {
        ///          &quot;name&quot;: &quot;Look&quot;,
        ///          &quot;type&quot;: &quot;Value&quot;,
        ///          &quot;id&quot;: &quot;c63a6ade-6c5a-4659-9aa5-e336e7b9970f&quot;,
        ///          &quot;expectedControlType&quot;: &quot;Vector2&quot;,
        ///          &quot;processors&quot;: &quot;AxisDeadzone(max=1)&quot;,
        ///          &quot;interactions&quot;: &quot;&quot;,
        ///          &quot;initialStateCheck&quot;: true
        ///        },
        ///        {
        ///          &quot;name&quot;: &quot;Move&quot;,
        ///          &quot;type&quot;: &quot;Value&quot;,        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string lc_inputs {
            get {
                return ResourceManager.GetString("lc_inputs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] lethalcompanyvr {
            get {
                object obj = ResourceManager.GetObject("lethalcompanyvr", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///        &quot;name&quot;: &quot;OpenXR XR Plugin&quot;,
        ///        &quot;version&quot;: &quot;1.8.2&quot;,
        ///        &quot;libraryName&quot;: &quot;UnityOpenXR&quot;,
        ///        &quot;displays&quot;: [
        ///                {
        ///                        &quot;id&quot;: &quot;OpenXR Display&quot;
        ///                }
        ///        ],
        ///        &quot;inputs&quot;: [
        ///                {
        ///                        &quot;id&quot;: &quot;OpenXR Input&quot;
        ///                }
        ///        ]
        ///}.
        /// </summary>
        internal static string UnitySubsystemsManifest {
            get {
                return ResourceManager.GetString("UnitySubsystemsManifest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///    &quot;name&quot;: &quot;VR&quot;,
        ///    &quot;maps&quot;: [
        ///        {
        ///            &quot;name&quot;: &quot;Head&quot;,
        ///            &quot;id&quot;: &quot;7f3d9a5f-aadc-4a0b-9b79-b32e1b5afa1c&quot;,
        ///            &quot;actions&quot;: [
        ///                {
        ///                    &quot;name&quot;: &quot;Position&quot;,
        ///                    &quot;type&quot;: &quot;Value&quot;,
        ///                    &quot;id&quot;: &quot;3bbc2aad-20de-4984-9d68-83cb6f68ce5b&quot;,
        ///                    &quot;expectedControlType&quot;: &quot;Vector3&quot;,
        ///                    &quot;processors&quot;: &quot;&quot;,
        ///                    &quot;interactions&quot;: &quot;&quot;,
        ///                    &quot;initialStateCheck&quot;: true
        ///                }, [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string vr_inputs {
            get {
                return ResourceManager.GetString("vr_inputs", resourceCulture);
            }
        }
    }
}
