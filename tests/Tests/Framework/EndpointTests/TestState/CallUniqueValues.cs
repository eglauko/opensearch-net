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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OpenSearch.Net;

namespace Tests.Framework.EndpointTests.TestState
{
	/// <summary>
	/// Holds unique values for the the two DSL's and the exposed sync and async methods we expose
	/// <see cref="ClientMethod" />
	/// </summary>
	public class CallUniqueValues : Dictionary<ClientMethod, string>
	{
		private readonly string _prefix;

		public CallUniqueValues(string prefix = "osc")
		{
			_prefix = prefix;
			FixedForAllCallsValue = UniqueValue;
			SetupClientMethod(ClientMethod.Fluent);
			SetupClientMethod(ClientMethod.FluentAsync);
			SetupClientMethod(ClientMethod.Initializer);
			SetupClientMethod(ClientMethod.InitializerAsync);
			CurrentView = ClientMethod.Fluent;
		}

		public ClientMethod CurrentView { get; set; }
		public string FixedForAllCallsValue { get; }

		public string Value => this[CurrentView];
		public string ViewName => CurrentView.GetStringValue().ToLowerInvariant();

		public ClientMethod[] Views { get; } = { ClientMethod.Fluent, ClientMethod.FluentAsync, ClientMethod.Initializer, ClientMethod.InitializerAsync };

		private IDictionary<ClientMethod, ConcurrentDictionary<string, object>> ExtendedValues { get; }
			= new Dictionary<ClientMethod, ConcurrentDictionary<string, object>>();

		private string UniqueValue => $"{_prefix}-{ViewName}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

		public T ExtendedValue<T>(string key) where T : class => ExtendedValues[CurrentView][key] as T;

		public bool TryGetExtendedValue<T>(string key, out T t) where T : class
		{
			var tryGetValue = ExtendedValues[CurrentView].TryGetValue(key, out var o);
			t = o as T;
			return tryGetValue;
		}

		public void ExtendedValue<T>(string key, T value) where T : class => ExtendedValues[CurrentView][key] = value;

		public T ExtendedValue<T>(string key, Func<T> value) where T : class =>
			ExtendedValues[CurrentView].GetOrAdd(key, value) as T;

		private void SetupClientMethod(ClientMethod method)
		{
			CurrentView = method;
			Add(method, UniqueValue);
			ExtendedValues.Add(method, new ConcurrentDictionary<string, object>());
		}
	}
}
