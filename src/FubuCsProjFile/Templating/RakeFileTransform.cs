﻿using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuCore.CommandLine;

namespace FubuCsProjFile.Templating
{
    public class RakeFileTransform : ITemplateStep
    {
        private readonly string _text;
        public static readonly string TargetFile = "rakefile";
        public static readonly string SourceFile = "rake.txt";

        public RakeFileTransform(string text)
        {
            _text = text;
        }

        public void Alter(TemplatePlan plan)
        {
            var lines = plan.ApplySubstitutions(_text).SplitOnNewLine();

            var rakeFile = plan.Root.AppendPath(TargetFile);
            var fileSystem = new FileSystem();


            var list =
                fileSystem.FileExists(rakeFile)
                    ? fileSystem.ReadStringFromFile(rakeFile)
                                .ReadLines().ToList()
                    : new List<string>();

            if (list.ContainsSequence(lines))
            {
                return;
            }
            else
            {
                list.Add(string.Empty);
                list.AddRange(lines);

                fileSystem.WriteStringToFile(rakeFile, list.Join(Environment.NewLine));
            }
        }

        public override string ToString()
        {
            return "Add content to the rakefile:";
        }
    }
}