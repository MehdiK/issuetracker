using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Should;
using TestStack.BDDfy;
using WebApiBook.IssueTrackerApi.Infrastructure;
using WebApiBook.IssueTrackerApi.Models;
using Xbehave;
using Xunit;
using TestStack.BDDfy.Scanners.StepScanners.Fluent;

namespace WebApiBook.IssueTrackerApp.AcceptanceTests.Features
{
    public class RetrievingIssues : IssuesFeature
    {
        private Uri _uriIssues = new Uri("http://localhost/issue");
        private Uri _uriIssue1 = new Uri("http://localhost/issue/1");
        private Uri _uriIssue2 = new Uri("http://localhost/issue/2");
        private IssuesState _issuesState;
        private IssueState _issueState;
        private Issue _fakeIssue;

        void GivenAnExistingIssue(string issueId)
        {
            MockIssueStore.Setup(i => i.FindAsync(issueId)).Returns(Task.FromResult(_fakeIssue));
        }

        void GivenExistingIssues()
        {
            MockIssueStore.Setup(i => i.FindAsync()).Returns(Task.FromResult(FakeIssues));
        }

        void WhenAllIssuesAreRetrieved()
        {
            Request.RequestUri = _uriIssues;
            Response = Client.SendAsync(Request).Result;
            _issuesState = Response.Content.ReadAsAsync<IssuesState>().Result;
        }

        void ThenHttpStatusCodeIs(HttpStatusCode statusCode)
        {
            Response.StatusCode.ShouldEqual(statusCode);
        }

        void AndAllIssuesAreReturned()
        {
            _issuesState.Issues.FirstOrDefault(i => i.Id == "1").ShouldNotBeNull();
            _issuesState.Issues.FirstOrDefault(i => i.Id == "2").ShouldNotBeNull();
        }

        void WhenItIsRetrieved(Uri uri)
        {
            Request.RequestUri = uri;
            Response = Client.SendAsync(Request).Result;
            if(Response.Content != null)
                _issueState = Response.Content.ReadAsAsync<IssueState>().Result;
        }

        void AndIssueStateIsReturned()
        {
            _issueState.ShouldNotBeNull();
        }

        void WithAnId()
        {
            _issueState.Id.ShouldEqual(_fakeIssue.Id);
        }

        void AndATitle()
        {
            _issueState.Title.ShouldEqual(_fakeIssue.Title);
        }

        void AndADescription()
        {
            _issueState.Description.ShouldEqual(_fakeIssue.Description);
        }

        void AndTheCorrectState()
        {
            _issueState.Status.ShouldEqual(_fakeIssue.Status);
        }

        void AndASelfLink()
        {
            var link = _issueState.Links.FirstOrDefault(l => l.Rel == IssueLinkFactory.Rels.Self);
            link.ShouldNotBeNull();
            link.Href.AbsoluteUri.ShouldEqual("http://localhost/issue/1");
        }

        void AndATransitionLink()
        {
            var link = _issueState.Links.FirstOrDefault(l => l.Rel == IssueLinkFactory.Rels.IssueProcessor && l.Action == IssueLinkFactory.Actions.Transition);
            link.ShouldNotBeNull();
            link.Href.AbsoluteUri.ShouldEqual("http://localhost/issueprocessor/1?action=transition");
        }

        void ThenItShouldHaveACloseActionLink()
        {
            var link = _issueState.Links.FirstOrDefault(
                    l => l.Rel == IssueLinkFactory.Rels.IssueProcessor && l.Action == IssueLinkFactory.Actions.Close);
            link.ShouldNotBeNull();
            link.Href.AbsoluteUri.ShouldEqual("http://localhost/issueprocessor/1?action=close");
        }

        void ThenItShouldHaveAnOpenActionLink()
        {
            var link =
                _issueState.Links.FirstOrDefault(
                    l => l.Rel == IssueLinkFactory.Rels.IssueProcessor && l.Action == IssueLinkFactory.Actions.Open);
            link.ShouldNotBeNull();
            link.Href.AbsoluteUri.ShouldEqual("http://localhost/issueprocessor/2?action=open");
        }

        [Fact]
        public void RetrievingAllIssues()
        {
            this.Given(x => x.GivenExistingIssues())
                .When(x => x.WhenAllIssuesAreRetrieved())
                .Then(x => x.ThenHttpStatusCodeIs(HttpStatusCode.OK), "Then a '200 OK' status is returned")
                   .And(x => x.AndAllIssuesAreReturned())
                .BDDfy();
        }

        [Fact]
        public void RetrievingAnIssue()
        {
            _fakeIssue = FakeIssues.FirstOrDefault();
            this.Given(x => x.GivenAnExistingIssue("1"), false)
                .When(x => x.WhenItIsRetrieved(_uriIssue1), false)
                .Then(x => x.ThenHttpStatusCodeIs(HttpStatusCode.OK), "Then a '200 OK' status is returned")
                    .And(x => x.AndIssueStateIsReturned())
                    .And(x => x.WithAnId())
                    .And(x => x.AndATitle())
                    .And(x => x.AndADescription())
                    .And(x => x.AndTheCorrectState())
                    .And(x => x.AndASelfLink())
                    .And(x => x.AndATransitionLink())
                .BDDfy();
        }

        [Fact]
        public void RetrievingAnOpenIssue()
        {
            _fakeIssue = FakeIssues.Single(i => i.Status == IssueStatus.Open);
            this.Given(x => x.GivenAnExistingIssue("1"), false)
                .When(x => x.WhenItIsRetrieved(_uriIssue1), false)
                .Then(x => x.ThenItShouldHaveACloseActionLink())
                .BDDfy();
        }

        [Fact]
        public void RetrievingAClosedIssue()
        {
            _fakeIssue = FakeIssues.Single(i => i.Status == IssueStatus.Closed);

            this.Given(x => x.GivenAnExistingIssue("2"), "Given an existing closed issue")
                .When(x => x.WhenItIsRetrieved(_uriIssue2), false)
                .Then(x => x.ThenItShouldHaveAnOpenActionLink())
                .BDDfy();
        }

        [Fact]
        public void RetrievingAnIssueThatDoesNotExist()
        {
            _fakeIssue = null;
            this.Given(x => x.GivenAnExistingIssue("1"), "Given an issue does not exist")
                .When(x => x.WhenItIsRetrieved(_uriIssue1), false)
                .Then(x => x.ThenHttpStatusCodeIs(HttpStatusCode.NotFound), "Then a '404 Not Found' status is returned")
                .BDDfy();
        }
    }
}
