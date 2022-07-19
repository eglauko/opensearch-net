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
using System.Runtime.Serialization;
using OpenSearch.Net;

namespace OpenSearch.Client
{
	[StringEnum]
	public enum TimeUnit
	{
		[EnumMember(Value = "nanos")]
		Nanoseconds,

		[EnumMember(Value = "micros")]
		Microseconds,

		[EnumMember(Value = "ms")]
		Millisecond,

		[EnumMember(Value = "s")]
		Second,

		[EnumMember(Value = "m")]
		Minute,

		[EnumMember(Value = "h")]
		Hour,

		[EnumMember(Value = "d")]
		Day
	}

	public static class TimeUnitExtensions
	{
		public static string GetStringValue(this TimeUnit value)
		{
			switch (value)
			{
				case TimeUnit.Nanoseconds:
					return "nanos";
				case TimeUnit.Microseconds:
					return "micros";
				case TimeUnit.Millisecond:
					return "ms";
				case TimeUnit.Second:
					return "s";
				case TimeUnit.Minute:
					return "m";
				case TimeUnit.Hour:
					return "h";
				case TimeUnit.Day:
					return "d";
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
			}
		}
	}
}