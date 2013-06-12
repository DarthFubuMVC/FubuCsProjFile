﻿using System.Diagnostics;
using System.IO;
using FubuCore;
using NUnit.Framework;
using FubuTestingSupport;
using System.Collections.Generic;
using System.Linq;

namespace FubuCsProjFile.Testing
{
    [TestFixture]
    public class SolutionTester
    {
        [Test]
        public void create_new_and_read_preamble()
        {
            var solution = Solution.CreateNew(".", "foo");
            solution.Preamble.ShouldHaveTheSameElementsAs("Microsoft Visual Studio Solution File, Format Version 11.00", "# Visual Studio 2010");
        }

        [Test]
        public void create_new_and_read_build_configurations()
        {
            var solution = Solution.CreateNew(".", "foo");
            solution.Configurations().ShouldHaveTheSameElementsAs(
                new BuildConfiguration("Debug|Any CPU = Debug|Any CPU"),
                new BuildConfiguration("Debug|Mixed Platforms = Debug|Mixed Platforms"),
                new BuildConfiguration("Debug|x86 = Debug|x86"),
                new BuildConfiguration("Release|Any CPU = Release|Any CPU"),
                new BuildConfiguration("Release|Mixed Platforms = Release|Mixed Platforms"),
                new BuildConfiguration("Release|x86 = Release|x86")
                
                
                
                );
        }

        [Test]
        public void create_new_and_read_other_globals()
        {
            var solution = Solution.CreateNew(".", "foo");
            solution.FindSection("SolutionProperties").Properties
                .ShouldHaveTheSameElementsAs("HideSolutionNode = FALSE");
        }

        [Test]
        public void write_a_solution()
        {
            var solution = Solution.CreateNew(".".ToFullPath(), "foo");
            solution.Save();

            var original =
                new FileSystem().ReadStringFromFile(
                    ".".ToFullPath()
                       .ParentDirectory()
                       .ParentDirectory()
                       .ParentDirectory()
                       .AppendPath("FubuCsProjFile", "Solution.txt")).SplitOnNewLine();

            var newContent = new FileSystem().ReadStringFromFile("foo.sln").SplitOnNewLine();

            newContent.ShouldHaveTheSameElementsAs(original);

        }

        [Test]
        public void read_a_solution_with_projects()
        {
            var solution = Solution.LoadFrom("FubuMVC.SlickGrid.sln");
            solution.Projects.Select(x => x.ProjectName)
                .ShouldHaveTheSameElementsAs("Solution Items", "FubuMVC.SlickGrid", "FubuMVC.SlickGrid.Testing", "SlickGridHarness", "FubuMVC.SlickGrid.Serenity", "FubuMVC.SlickGrid.Docs");
        }

        [Test]
        public void read_and_write_a_solution_with_projects()
        {
            var solution = Solution.LoadFrom("FubuMVC.SlickGrid.sln");
            solution.Save("fake.sln");

            var original =
                new FileSystem().ReadStringFromFile("FubuMVC.SlickGrid.sln").Trim().SplitOnNewLine()
                .Select(x => x.Replace('\\', Path.DirectorySeparatorChar));

            var newContent = new FileSystem().ReadStringFromFile("fake.sln").SplitOnNewLine();

            newContent.Each(x => Debug.WriteLine(x));

            newContent.ShouldHaveTheSameElementsAs(original);
        }
    }
}