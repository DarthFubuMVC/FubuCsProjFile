﻿using System;
using System.Collections.Generic;

namespace FubuCsProjFile.ProjectFiles
{
    public interface IProjectFile
    {
        Guid Type { get; }
        string ProjectName { get; }
        string FileName { get; }
        Guid ProjectGuid { get; }
        string AssemblyName { get; set; }
        string RootNamespace { get; set; }
        string DotNetVersion { get; set; }
        string ProjectDirectory { get; }
        SourceControlInformation SourceControlInformation { get; set; }
        IEnumerable<Guid> ProjectTypes();
        void Save();
        void Save(string filename);
        T Find<T>(string arg) where T : ProjectItem, new();
        T Add<T>(string arg) where T : ProjectItem, new();
        void Add<T>(T arg) where T : ProjectItem;
        string PathTo(CodeFile arg);
        IEnumerable<T> All<T>() where T : ProjectItem, new();
        void Remove<T>(string arg) where T : ProjectItem, new();
        void Remove<T>(T arg) where T : ProjectItem;
    }
}