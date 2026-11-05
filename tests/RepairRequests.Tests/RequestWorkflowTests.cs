using RepairRequests.Core.Models;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

public class RequestWorkflowTests
{
    private static RepairRequest NewRequest() => new()
    {
        Id = 1,
        Number = "ЗР-2026-0001",
        Description = "Плановая диагностика ходовой части автомобиля",
        Status = RequestStatus.New,
        CreatedAt = new DateTime(2026, 1, 10, 9, 0, 0)
    };

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.Accepted, true)]
    [InlineData(RequestStatus.New, RequestStatus.Cancelled, true)]
    [InlineData(RequestStatus.New, RequestStatus.Completed, false)]
    [InlineData(RequestStatus.Accepted, RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.Accepted, RequestStatus.Closed, false)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed, true)]
    [InlineData(RequestStatus.Completed, RequestStatus.Closed, true)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress, false)]
    [InlineData(RequestStatus.Closed, RequestStatus.New, false)]
    [InlineData(RequestStatus.Cancelled, RequestStatus.New, false)]
    public void CanTransition_MatchesLifecycleRules(RequestStatus from, RequestStatus to, bool expected)
    {
        Assert.Equal(expected, RequestWorkflow.CanTransition(from, to));
    }

    [Fact]
    public void NextStatuses_ReturnsEmpty_ForTerminalStates()
    {
        Assert.Empty(RequestWorkflow.NextStatuses(RequestStatus.Closed));
        Assert.Empty(RequestWorkflow.NextStatuses(RequestStatus.Cancelled));
    }

    [Fact]
    public void Apply_FullHappyPath_SetsTimestampsAndHistory()
    {
        var request = NewRequest();

        RequestWorkflow.Apply(request, RequestStatus.Accepted, changedById: 2);
        Assert.Equal(RequestStatus.Accepted, request.Status);
        Assert.NotNull(request.AcceptedAt);

        RequestWorkflow.Apply(request, RequestStatus.InProgress, changedById: 2);
        Assert.NotNull(request.StartedAt);

        RequestWorkflow.Apply(request, RequestStatus.Completed, changedById: 2);
        Assert.NotNull(request.CompletedAt);

        RequestWorkflow.Apply(request, RequestStatus.Closed, changedById: 3);
        Assert.NotNull(request.ClosedAt);

        Assert.Equal(4, request.History.Count);
        Assert.Equal(
            new[]
            {
                RequestStatus.Accepted, RequestStatus.InProgress,
                RequestStatus.Completed, RequestStatus.Closed
            },
            request.History.Select(h => h.Status));
        Assert.All(request.History, h => Assert.True(h.ChangedAt > DateTime.MinValue));
        Assert.Equal(new int?[] { 2, 2, 2, 3 }, request.History.Select(h => h.ChangedById));
    }

    [Fact]
    public void Apply_Throws_ForIllegalTransition()
    {
        var request = NewRequest();

        var error = Assert.Throws<InvalidOperationException>(
            () => RequestWorkflow.Apply(request, RequestStatus.Completed, changedById: 2));

        Assert.Contains("недопустим", error.Message);
        Assert.Equal(RequestStatus.New, request.Status);
        Assert.Empty(request.History);
    }

    [Fact]
    public void Apply_CancelWithoutComment_Throws()
    {
        var request = NewRequest();

        var error = Assert.Throws<InvalidOperationException>(
            () => RequestWorkflow.Apply(request, RequestStatus.Cancelled, changedById: 1));

        Assert.Contains("причину", error.Message);
    }

    [Fact]
    public void Apply_CancelWithComment_SetsClosedAtAndComment()
    {
        var request = NewRequest();

        RequestWorkflow.Apply(request, RequestStatus.Cancelled, changedById: 1, comment: "Клиент отказался");

        Assert.Equal(RequestStatus.Cancelled, request.Status);
        Assert.NotNull(request.ClosedAt);
        var history = Assert.Single(request.History);
        Assert.Equal("Клиент отказался", history.Comment);
    }

    [Theory]
    [InlineData(RequestStatus.Accepted, "Принять заявку")]
    [InlineData(RequestStatus.InProgress, "Начать работы")]
    [InlineData(RequestStatus.Completed, "Завершить ремонт")]
    [InlineData(RequestStatus.Closed, "Закрыть заявку")]
    [InlineData(RequestStatus.Cancelled, "Отменить заявку")]
    public void ActionName_ReturnsRussianCaption(RequestStatus status, string expected)
    {
        Assert.Equal(expected, RequestWorkflow.ActionName(status));
    }
}
