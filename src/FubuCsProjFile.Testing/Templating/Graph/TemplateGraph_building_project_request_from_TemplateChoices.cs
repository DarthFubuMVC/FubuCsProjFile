﻿using System;
using System.Collections.Generic;
using FubuCsProjFile.Templating.Graph;
using FubuTestingSupport;
using NUnit.Framework;

namespace FubuCsProjFile.Testing.Templating.Graph
{
    [TestFixture]
    public class TemplateGraph_building_project_request_from_TemplateChoices
    {
        [Test]
        public void build_request_with_default_value_for_a_selection()
        {
            var graph = new TemplateGraph();
            var templateSet = new TemplateSet
            {
                Name = "Foo",
                Tags = new List<string> {"A", "B"},
                Template = "FooProj",
                Alterations = new List<string> {"Foo1", "Foo2"}
            };
            graph.Add(templateSet);

            templateSet.Selections.Add(new OptionSelection
            {
                Name = "FooSelection",
                Options = new List<Option>
                {
                    new Option
                    {
                        Name = "FooOpt1",
                        Alterations = new List<string> {"C", "D"}
                    },
                    new Option
                    {
                        Name = "FooOpt2",
                        Alterations = new List<string> {"E", "F"}
                    }
                }
            });


            var choices = new TemplateChoices {SetName = "Foo", Tag = "A", ProjectName = "MyFoo"};

            ProjectRequest request = graph.Configure(choices);
            request.Alterations.ShouldHaveTheSameElementsAs("Foo1", "Foo2", "C", "D");
        }

        [Test]
        public void build_request_with_named_value_for_a_selection()
        {
            var graph = new TemplateGraph();
            var templateSet = new TemplateSet
            {
                Name = "Foo",
                Tags = new List<string> { "A", "B" },
                Template = "FooProj",
                Alterations = new List<string> { "Foo1", "Foo2" }
            };
            graph.Add(templateSet);

            templateSet.Selections.Add(new OptionSelection
            {
                Name = "FooSelection",
                Options = new List<Option>
                {
                    new Option
                    {
                        Name = "FooOpt1",
                        Alterations = new List<string> {"C", "D"}
                    },
                    new Option
                    {
                        Name = "FooOpt2",
                        Alterations = new List<string> {"E", "F"}
                    }
                }
            });


            var choices = new TemplateChoices { SetName = "Foo", Tag = "A", ProjectName = "MyFoo" };
            choices.Selections["FooSelection"] = "fooopt2";

            ProjectRequest request = graph.Configure(choices);
            request.Alterations.ShouldHaveTheSameElementsAs("Foo1", "Foo2", "E", "F");
        }

        [Test]
        public void build_request_with_matching_template_and_options()
        {
            var graph = new TemplateGraph();
            graph.Add(new TemplateSet
            {
                Name = "Foo",
                Tags = new List<string> {"A", "B"},
                Template = "FooProj",
                Alterations = new List<string> {"Foo1", "Foo2"}
            });

            var choices = new TemplateChoices {SetName = "Foo", Tag = "A", ProjectName = "MyFoo"};

            ProjectRequest request = graph.Configure(choices);

            request.Name.ShouldEqual(choices.ProjectName);
            request.Template.ShouldEqual("FooProj");
            request.Alterations.ShouldHaveTheSameElementsAs("Foo1", "Foo2");
        }

        [Test]
        public void build_request_copies_inputs_around_substitutions()
        {
            var graph = new TemplateGraph();
            graph.Add(new TemplateSet
            {
                Name = "Foo",
                Tags = new List<string> { "A", "B" },
                Template = "FooProj",
                Alterations = new List<string> { "Foo1", "Foo2" }
            });

            var choices = new TemplateChoices { SetName = "Foo", Tag = "A", ProjectName = "MyFoo" };
            choices.Inputs["Foo1"] = "A";
            choices.Inputs["Foo2"] = "B";
            choices.Inputs["Foo3"] = "C";

            ProjectRequest request = graph.Configure(choices);
            request.Substitutions.ValueFor("Foo1").ShouldEqual("A");
            request.Substitutions.ValueFor("Foo2").ShouldEqual("B");
            request.Substitutions.ValueFor("Foo3").ShouldEqual("C");
        }

        [Test]
        public void build_request_with_options()
        {
            var graph = new TemplateGraph();
            var templateSet = new TemplateSet
            {
                Name = "Foo",
                Tags = new List<string> {"A", "B"},
                Template = "FooProj",
                Alterations = new List<string> {"Foo1", "Foo2"}
            };
            graph.Add(templateSet);

            templateSet.Options.Add(new Option
            {
                Name = "FooOpt1",
                Alterations = new List<string> {"C", "D"}
            });

            templateSet.Options.Add(new Option
            {
                Name = "FooOpt2",
                Alterations = new List<string> {"E", "F"}
            });

            templateSet.Options.Add(new Option
            {
                Name = "FooOpt3",
                Alterations = new List<string> {"G", "H"}
            });

            var choices = new TemplateChoices
            {
                SetName = "Foo",
                Tag = "A",
                ProjectName = "MyFoo",
                Options = new[] {"FooOpt1", "FooOpt3"}
            };

            ProjectRequest request = graph.Configure(choices);
            request.Alterations.ShouldHaveTheSameElementsAs("Foo1", "Foo2", "C", "D", "G", "H");
        }

        [Test]
        public void throw_if_found_generation_set_does_not_match_the_tag()
        {
            var graph = new TemplateGraph();
            graph.Add(new TemplateSet
            {
                Name = "Foo",
                Tags = new List<string> {"A", "B"}
            });

            Exception<Exception>.ShouldBeThrownBy(
                () => { graph.Configure(new TemplateChoices {SetName = "Foo", Tag = "C", ProjectName = "MyFoo"}); })
                .Message.ShouldContain("TemplateSet 'Foo' is not tagged as a valid 'C'");
        }

        [Test]
        public void throw_if_generation_name_is_empty()
        {
            Exception<Exception>.ShouldBeThrownBy(() => { new TemplateGraph().Configure(new TemplateChoices()); })
                .Message.ShouldContain("SetName is required");
        }

        [Test]
        public void throw_if_named_generation_cannot_be_found()
        {
            Exception<Exception>.ShouldBeThrownBy(
                () => { new TemplateGraph().Configure(new TemplateChoices {SetName = "Foo", ProjectName = "MyFoo"}); })
                .Message.ShouldContain("TemplateSet 'Foo' is unknown");
        }

        [Test]
        public void throw_if_project_name_cannot_be_found()
        {
            Exception<Exception>.ShouldBeThrownBy(
                () => { new TemplateGraph().Configure(new TemplateChoices {SetName = "Foo"}); })
                .Message.ShouldContain("ProjectName is required");
        }
    }
}