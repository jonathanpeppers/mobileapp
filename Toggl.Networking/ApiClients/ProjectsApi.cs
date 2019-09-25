using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Networking.Models;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Shared.Models;

namespace Toggl.Networking.ApiClients
{
    internal sealed class ProjectsApi : BaseApi, IProjectsApi
    {
        private readonly ProjectEndpoints endPoints;
        private readonly Network.Reports.ProjectEndpoints reportsEndPoints;

        public ProjectsApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Projects;
            this.reportsEndPoints = endPoints.ReportsEndpoints.Projects;
        }

        public Task<List<IProject>> GetAll()
            => SendRequest<Project, IProject>(endPoints.Get, AuthHeader);

        public Task<List<IProject>> GetAllSince(DateTimeOffset threshold)
            => SendRequest<Project, IProject>(endPoints.GetSince(threshold, includeArchived: true), AuthHeader);

        public Task<IProject> Create(IProject project)
        {
            var endPoint = endPoints.Post(project.WorkspaceId);
            var projectCopy = project as Project ?? new Project(project);
            return SendRequest(endPoint, AuthHeader, projectCopy, SerializationReason.Post)
                .Upcast<IProject, Project>();
        }

        public Task<List<IProject>> Search(long workspaceId, long[] projectIds)
        {
            Ensure.Argument.IsNotNull(projectIds, nameof(projectIds));

            var json = $"{{\"ids\":[{string.Join(",", projectIds)}]}}";
            return SendRequest<Project, IProject>(reportsEndPoints.Search(workspaceId), AuthHeader, json);
        }
    }
}
