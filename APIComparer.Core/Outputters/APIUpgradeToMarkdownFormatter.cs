﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace APIComparer.Outputters
{
    public class APIUpgradeToMarkdownFormatter : IOutputter
    {
        private readonly string markdownFilePath;

        public APIUpgradeToMarkdownFormatter(string markdownFilePath)
        {
            this.markdownFilePath = markdownFilePath;
        }

        public void WriteOut(Diff diff)
        {
            var sb = new StringBuilder();

            var missingTypes = MissingMembers(diff.LeftOrphanTypes, diff.MatchingTypeDiffs.Select(td => Tuple.Create(td.LeftType, td.RightType)))
                .Select(t => t.FullName)
                .OrderBy(s => s);

            sb.AppendLine("The following types are missing in the new API.");
            sb.AppendLine();
            foreach (var missingType in missingTypes)
            {
                sb.AppendLine("    " + missingType);
            }

            sb.AppendLine();

            sb.AppendLine("The following members are missing on the public types.");
            sb.AppendLine();
            foreach (var typeDiff in diff.MatchingTypeDiffs.OrderBy(m => m.LeftType.FullName))
            {
                WriteOut(typeDiff, sb);
            }

            File.WriteAllText(markdownFilePath, sb.ToString());
        }

        private void WriteOut(TypeDiff typeDiff, StringBuilder sb)
        {
            var missingFields = MissingMembers(typeDiff.LeftOrphanFields, typeDiff.MatchingFields).Select(f => f.FullName).OrderBy(s => s);
            var missingMethods = MissingMembers(typeDiff.LeftOrphanMethods.Where(FilterMethods), typeDiff.MatchingMethods.Where(m => FilterMethods(m.Item1))).Select(f => f.FullName).OrderBy(s => s);

            if (missingFields.Any() || missingMethods.Any())
            {
                sb.AppendLine("    " + typeDiff.LeftType.FullName);

                foreach (var missingField in missingFields)
                {
                    sb.AppendLine("        " + missingField);
                }

                foreach (var missingMethod in missingMethods)
                {
                    sb.AppendLine("        " + missingMethod);
                }

                sb.AppendLine();
            }
        }

        private IEnumerable<T> MissingMembers<T>(IEnumerable<T> members, IEnumerable<Tuple<T, T>> matching) where T : IMemberDefinition
        {
            return members
                .Where(m => m.IsPublic() && !m.HasObsoleteAttribute())
                .Concat(matching
                    .Where(t => (t.Item1.IsPublic() && !t.Item1.HasObsoleteAttribute() && !t.Item2.IsPublic()))
                    .Select(t => t.Item1));
        }

        private bool FilterMethods(MethodDefinition method)
        {
            return !method.IsConstructor // Not constructors
                && (!method.IsVirtual || !method.IsReuseSlot); // Not public overrides
        }
    }
}