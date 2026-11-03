using System.Windows;
using Hoblio.Model;
using Hoblio.Studio.ViewModels;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class FlowConnectionWaypointTests
{
    static FlowConnectionViewModel Link(Point src, Point tgt, params Point[] waypoints)
    {
        var a = new FlowNodeViewModel { Id = "a", Kind = NodeKind.Action, Location = src };
        var b = new FlowNodeViewModel { Id = "b", Kind = NodeKind.Action, Location = tgt };
        var conn = new FlowConnectionViewModel { Source = a, Target = b };
        foreach (var wp in waypoints)
            conn.Waypoints.Add(wp);
        return conn;
    }

    [Fact]
    public void HandlePoints_match_react_flow_anchor_list()
    {
        var conn = Link(new Point(0, 0), new Point(200, 0), new Point(100, 80));
        var handles = conn.HandlePoints;
        Assert.Equal(3, handles.Count);
        Assert.Equal(new Point(100, 80), handles[1]);
    }

    [Fact]
    public void Without_manual_waypoints_one_logical_insert_segment()
    {
        var conn = Link(new Point(0, 0), new Point(400, 120));
        Assert.False(conn.HasManualWaypoints);
        Assert.Equal(2, conn.HandlePoints.Count);
        Assert.Equal(1, conn.HandlePoints.Count - 1);
    }
}
