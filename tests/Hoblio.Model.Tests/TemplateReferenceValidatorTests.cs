using Hoblio.Model;

namespace Hoblio.Model.Tests;

public class TemplateReferenceValidatorTests
{
    [Fact]
    public void Unknown_ref_in_log_message_fails_validate_project()
    {
        var project = new FlowProject
        {
            Manifest = new FlowManifest { Name = "test", Version = "1" },
            Flow = new FlowGraph
            {
                Start = "start",
                Nodes =
                [
                    new Node { Id = "start", Kind = NodeKind.Start },
                    new Node { Id = "log1", Kind = NodeKind.Action, Action = new LogAction { Message = "hello {missing}" } },
                    new Node { Id = "end", Kind = NodeKind.End },
                ],
                Edges =
                [
                    new Edge { From = "start", To = "log1" },
                    new Edge { From = "log1", To = "end" },
                ],
            },
            Variables = [],
        };

        var ex = Assert.Throws<ValidationException>(() => GraphValidator.ValidateProject(project));
        Assert.Equal(ValidationErrorKind.UnknownTemplateReference, ex.Kind);
    }

    [Fact]
    public void Producer_ref_in_later_node_is_allowed()
    {
        var project = new FlowProject
        {
            Manifest = new FlowManifest { Name = "test", Version = "1" },
            Flow = new FlowGraph
            {
                Start = "start",
                Nodes =
                [
                    new Node { Id = "start", Kind = NodeKind.Start },
                    new Node
                    {
                        Id = "read",
                        Kind = NodeKind.Action,
                        Action = new FileReadAction { Path = "a.txt", Variable = "content" },
                    },
                    new Node { Id = "log1", Kind = NodeKind.Action, Action = new LogAction { Message = "{content}" } },
                    new Node { Id = "end", Kind = NodeKind.End },
                ],
                Edges =
                [
                    new Edge { From = "start", To = "read" },
                    new Edge { From = "read", To = "log1" },
                    new Edge { From = "log1", To = "end" },
                ],
            },
            Variables = [],
        };

        GraphValidator.ValidateProject(project);
    }
}
