using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sachiel.Messages.Packets
{

    public enum SchemaOptimization
    {
        SPEED,
        CODE_SIZE,
        LITE_RUNTIME
    }

    public class SchemaOptions
    {
        public SchemaOptions() { }

        public SchemaOptions(string extension, bool removePackage, bool suffixKind, string javaPackage, bool javaMultipleFiles, SchemaOptimization optimizeFor, bool cCEnableArenas, string objCClassPrefix, bool saveCSharpNamespace, string cSharpNamespace, string goPackage)
        {
            Extension = extension;
            RemovePackage = removePackage;
            SuffixKind = suffixKind;
            JavaPackage = javaPackage;
            JavaMultipleFiles = javaMultipleFiles;
            OptimizeFor = optimizeFor;
            CCEnableArenas = cCEnableArenas;
            ObjCClassPrefix = objCClassPrefix;
            SaveCSharpNamespace = saveCSharpNamespace;
            CSharpNamespace = cSharpNamespace;
            GoPackage = goPackage;
        }

        public void BuildString(StringBuilder builder, Type type, bool isRequest)
        {
            builder.AppendLine($"option optimize_for = {Enum.GetName(typeof(SchemaOptimization), OptimizeFor)}");

            if (JavaPackage != null)
            {
                builder.AppendLine($"option java_package = \"{JavaPackage + (SuffixKind ? $".{(isRequest ? "request" : "response")}" : string.Empty)}\"");
            }

            if (JavaMultipleFiles == true)
            {
                builder.AppendLine($"option java_multiple_files = true");
            }

            if (CCEnableArenas == true)
            {
                builder.AppendLine($"option cc_enable_arenas = true");
            }

            if (ObjCClassPrefix != null)
            {
                builder.AppendLine($"option obj_class_prefix = \"{ObjCClassPrefix}\"");
            }

            if (SaveCSharpNamespace == true)
            {
                builder.AppendLine($"option csharp_namespace = \"{type.Namespace}\"");
            }
            else if (CSharpNamespace != null)
            {
                builder.AppendLine($"option csharp_namespace = \"{CSharpNamespace + (SuffixKind ? $".{(isRequest ? "request" : "response")}" : string.Empty)}\"");
            }

            if (GoPackage != null)
            {
                var go = GoPackage + (SuffixKind ? $"/{(isRequest ? "request" : "response")}" : string.Empty);
                builder.AppendLine($"option go_package = \"{go};{Path.GetFileName(go)}\"");
            }
        }

        public string Extension { get; set; } = "proto";
        public bool RemovePackage { get; set; } = false;
        public bool SuffixKind { get; set; } = false;
        public string JavaPackage { get; set; }
        public bool JavaMultipleFiles { get; set; }
        public SchemaOptimization OptimizeFor { get; set; } = SchemaOptimization.SPEED;
        public bool CCEnableArenas { get; set; }
        public string ObjCClassPrefix { get; set; }
        public bool SaveCSharpNamespace { get; set; }
        public string CSharpNamespace { get; set; }
        public string GoPackage { get; set; }

        public override bool Equals(object obj)
        {
            var options = obj as SchemaOptions;
            return options != null &&
                   Extension == options.Extension &&
                   RemovePackage == options.RemovePackage &&
                   SuffixKind == options.SuffixKind &&
                   JavaPackage == options.JavaPackage &&
                   JavaMultipleFiles == options.JavaMultipleFiles &&
                   OptimizeFor == options.OptimizeFor &&
                   CCEnableArenas == options.CCEnableArenas &&
                   ObjCClassPrefix == options.ObjCClassPrefix &&
                   SaveCSharpNamespace == options.SaveCSharpNamespace &&
                   CSharpNamespace == options.CSharpNamespace &&
                   GoPackage == options.GoPackage;
        }

        public override int GetHashCode()
        {
            var hashCode = 21633992;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Extension);
            hashCode = hashCode * -1521134295 + RemovePackage.GetHashCode();
            hashCode = hashCode * -1521134295 + SuffixKind.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(JavaPackage);
            hashCode = hashCode * -1521134295 + JavaMultipleFiles.GetHashCode();
            hashCode = hashCode * -1521134295 + OptimizeFor.GetHashCode();
            hashCode = hashCode * -1521134295 + CCEnableArenas.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ObjCClassPrefix);
            hashCode = hashCode * -1521134295 + SaveCSharpNamespace.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CSharpNamespace);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GoPackage);
            return hashCode;
        }
    };
}