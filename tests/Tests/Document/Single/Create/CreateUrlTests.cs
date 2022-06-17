/* SPDX-License-Identifier: Apache-2.0
*
* The OpenSearch Contributors require contributions made to
* this file be licensed under the Apache-2.0 license or a
* compatible open source license.
*
* Modifications Copyright OpenSearch Contributors. See
* GitHub history for details.
*
*  Licensed to Elasticsearch B.V. under one or more contributor
*  license agreements. See the NOTICE file distributed with
*  this work for additional information regarding copyright
*  ownership. Elasticsearch B.V. licenses this file to you under
*  the Apache License, Version 2.0 (the "License"); you may
*  not use this file except in compliance with the License.
*  You may obtain a copy of the License at
*
* 	http://www.apache.org/licenses/LICENSE-2.0
*
*  Unless required by applicable law or agreed to in writing,
*  software distributed under the License is distributed on an
*  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
*  KIND, either express or implied.  See the License for the
*  specific language governing permissions and limitations
*  under the License.
*/

using System.Threading.Tasks;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using OpenSearch.Client;
using Tests.Domain;
using Tests.Framework.EndpointTests;
using static Tests.Framework.EndpointTests.UrlTester;

namespace Tests.Document.Single.Create
{
	public class CreateUrlTests
	{
		[U] public async Task Urls()
		{
			var project = new Project { Name = "OSC" };

			await PUT("/project/_create/1")
				.Fluent(c => c.Create<object>(new { }, i => i.Index(typeof(Project)).Id(1)))
				.Request(c => c.Create(new CreateRequest<object>("project", 1) { Document = new { } }))
				.FluentAsync(c => c.CreateAsync<object>(new { }, i => i.Index(typeof(Project)).Id(1)))
				.RequestAsync(c => c.CreateAsync(new CreateRequest<object>(IndexName.From<Project>(), 1)
				{
					Document = new { }
				}));

			await PUT("/project/_create/OSC")
				.Fluent(c => c.CreateDocument(project))
				.Fluent(c => c.Create(project, f => f))
				.Request(c => c.Create(new CreateRequest<Project>(project)))
				.FluentAsync(c => c.CreateDocumentAsync(project))
				.FluentAsync(c => c.CreateAsync(project, f => f))
				.RequestAsync(c => c.CreateAsync(new CreateRequest<Project>(project)));

			await PUT("/project2/_create/OSC")
				.Fluent(c => c.Create(project, cc => cc.Index("project2")))
				.Request(c => c.Create(new CreateRequest<Project>(project, "project2", "OSC") { Document = project }))
				.RequestAsync(c => c.CreateAsync(new CreateRequest<Project>(project, "project2", "OSC") { Document = project }))
				.FluentAsync(c => c.CreateAsync(project, cc => cc.Index("project2")));

			await PUT("/different-projects/_create/opensearch")
					.Request(c => c.Create(new CreateRequest<Project>("different-projects", "opensearch") { Document = project }))
					.Request(c => c.Create(new CreateRequest<Project>(project, "different-projects", "opensearch")))
					.RequestAsync(c => c.CreateAsync(new CreateRequest<Project>(project, "different-projects", "opensearch")))
					.RequestAsync(c => c.CreateAsync(new CreateRequest<Project>("different-projects", "opensearch")
						{ Document = project }))
				;
		}
	}
}
