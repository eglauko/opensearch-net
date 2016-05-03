﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Newtonsoft.Json;
using Tests.ClientConcepts.LowLevel;
using Tests.Framework;
using Tests.Framework.MockData;
using static Tests.Framework.RoundTripper;
using static Nest.Infer;
using Field = Nest.Field;
using Xunit;

namespace Tests.ClientConcepts.HighLevel.Inference
{
	public class FieldInference
	{
		/**== Field Inference
		 *
		 * Several places in the Elasticsearch API expect the path to a field from your original source document as a string.
		 * NEST allows you to use C# expressions to strongly type these field path strings.
		 *
		 * These expressions are assigned to a type called `Field` and there are several ways to create an instance of one
		 */

		/**=== Constructor
		* Using the constructor directly is possible _but_ rather involved */
		[U]
		public void UsingConstructors()
		{
			var fieldString = new Field("name");

			var fieldProperty = new Field(typeof(Project).GetProperty(nameof(Project.Name)));

			Expression<Func<Project, object>> expression = p => p.Name;
			var fieldExpression = new Field(expression);

			Expect("name")
				.WhenSerializing(fieldExpression)
				.WhenSerializing(fieldString)
				.WhenSerializing(fieldProperty);
		}

		public class TestableFieldResolver : FieldResolver
		{
			public TestableFieldResolver(IConnectionSettingsValues settings) : base(settings) { }

			public long CachedFields => Fields.Count;
			public long CachedProperties => Properties.Count;
		}

		[U] public void CachingFieldsShouldNotLeak()
		{
			var resolver = new TestableFieldResolver(new ConnectionSettings());

			var field1 = Field<CommitActivity>(p => p.Id);
			var field2 = Field<CommitActivity>(p => p.Id);

			resolver.Resolve(field1);
			resolver.Resolve(field2);
			resolver.CachedFields.Should().Be(1,
				"resolved the same field twice albeit with different object references");

			var field3 = Field<CommitActivity>(p => p.Id, 2.2);
			resolver.Resolve(field3);
			resolver.CachedFields.Should().Be(1,
				"resolved the same field three times once with boosting which should have no baring on cache, suffixed independantly");

			var field4 = Field<Person>(p => p.Id);
			resolver.Resolve(field4);
			resolver.CachedFields.Should().Be(2,
				"same expression but on a different type");
		}

		[U]
		public void UsingConstructorAlsoSetsComparisonValue()
		{
			/** When using the constructor and passing a value for `Name`, `Property` or `Expression`,
			* `ComparisonValue` is also set on the `Field` instance; this is used when
			*
			* - determining `Field` equality
			* - getting the hash code for a `Field` instance
			*/
			var fieldStringWithBoostTwo = new Field("name^2");
			var fieldStringWithBoostThree = new Field("name^3");

			Expression<Func<Project, object>> expression = p => p.Name;
			var fieldExpression = new Field(expression);

			var fieldProperty = new Field(typeof(Project).GetProperty(nameof(Project.Name)));

			fieldStringWithBoostTwo.GetHashCode().Should().NotBe(0);
			fieldStringWithBoostThree.GetHashCode().Should().NotBe(0);
			fieldExpression.GetHashCode().Should().NotBe(0);
			fieldProperty.GetHashCode().Should().NotBe(0);

			fieldStringWithBoostTwo.Should().Be(fieldStringWithBoostThree); //<1> <<field-name-with-boost,Fields can constructed with a name that contains a boost>>
		}

		/**=== Implicit Conversion
		* As you can see from the previous examples, using the constructor is rather involved and cumbersome.
		* Because of this, you can also implicitly convert strings and expressions to a `Field` */
		[U]
		public void ImplicitConversion()
		{
			Field fieldString = "name";

			/** but for expressions this is _still_ rather involved */
			Expression<Func<Project, object>> expression = p => p.Name;
			Field fieldExpression = expression;

			Expect("name")
				.WhenSerializing(fieldExpression)
				.WhenSerializing(fieldString);
		}

		/**[[field-name-with-boost]]
		*=== Field Names with Boost
		*
		* When specifying a `Field` name, the name can include a boost value; NEST will split the name and boost
		* value and set the `Boost` property
		*/
		[U]
		public void NameCanSpecifyBoost()
		{
			Field fieldString = "name^2";
			Field fieldStringConstructor = new Field("name^2");
			Field fieldStringCreate = new Field("name^2", 3); //<1> NEST will take the boost from the name

			fieldString.Name.Should().Be("name");
			fieldStringConstructor.Name.Should().Be("name");
			fieldStringCreate.Name.Should().Be("name");
			fieldString.Boost.Should().Be(2);
			fieldStringConstructor.Boost.Should().Be(2);
			fieldStringCreate.Boost.Should().Be(2);
		}

		/**[[nest-infer]]
		* === Using Nest.Infer
		* To ease creating a `Field` instance from expressions, there is a static `Infer` class you can use
		*/
		[U]
		public void UsingStaticPropertyField()
		{
			Field fieldString = "name";

			/** but for expressions this is still rather involved */
			var fieldExpression = Infer.Field<Project>(p => p.Name);

			/** this can be even shortened even further using a https://msdn.microsoft.com/en-us/library/sf0df423.aspx#Anchor_0[static import in C# 6] i.e.
				`using static Nest.Infer;`
			*/
			fieldExpression = Field<Project>(p => p.Name);
			/** Now that is much terser then our first example using the constructor! */

			Expect("name")
				.WhenSerializing(fieldString)
				.WhenSerializing(fieldExpression);

			/** You can specify boosts in the field using a string */
			fieldString = "name^2.1";
			fieldString.Boost.Should().Be(2.1);

			/** As well as using `Nest.Infer.Field` */
			fieldExpression = Field<Project>(p => p.Name, 2.1);
			Expect("name^2.1")
				.WhenSerializing(fieldString)
				.WhenSerializing(fieldExpression);
		}

		/**[[camel-casing]]
		* === Field name casing
		* By default, NEST will camel-case **all** field names to better align with typical
		* javascript/json conventions
		*/
		[U]
		public void DefaultFieldNameInferrer()
		{
			/** using `DefaultFieldNameInferrer()` on ConnectionSettings you can change this behavior */
			var setup = WithConnectionSettings(s => s.DefaultFieldNameInferrer(p => p.ToUpper()));

			setup.Expect("NAME").WhenSerializing(Field<Project>(p => p.Name));

			/** However `string` types are *always* passed along verbatim */
			setup.Expect("NaMe").WhenSerializing<Field>("NaMe");

			/** if you want the same behavior for expressions, simply pass a Func<string,string> to `DefaultFieldNameInferrer`
			* to make no changes to the name
			*/
			setup = WithConnectionSettings(s => s.DefaultFieldNameInferrer(p => p));
			setup.Expect("Name").WhenSerializing(Field<Project>(p => p.Name));
		}

		/**=== Complex field name expressions */
		[U]
		public void ComplexFieldNameExpressions()
		{
			/** You can follow your property expression to any depth. Here we are traversing to the `LeadDeveloper` `FirstName` */
			Expect("leadDeveloper.firstName").WhenSerializing(Field<Project>(p => p.LeadDeveloper.FirstName));

			/** When dealing with collection indexers, the indexer access is ignored allowing you to traverse into properties of collections */
			Expect("curatedTags").WhenSerializing(Field<Project>(p => p.CuratedTags[0]));

			/** Similarly, LINQ's `.First()` method also works */
			Expect("curatedTags").WhenSerializing(Field<Project>(p => p.CuratedTags.First()));
			Expect("curatedTags.added").WhenSerializing(Field<Project>(p => p.CuratedTags[0].Added));
			Expect("curatedTags.name").WhenSerializing(Field<Project>(p => p.CuratedTags.First().Name));

			/** NOTE: Remember, these are _expressions_ and not actual code that will be executed
			*
			* An indexer on a dictionary is assumed to describe a property name */
			Expect("metadata.hardcoded").WhenSerializing(Field<Project>(p => p.Metadata["hardcoded"]));
			Expect("metadata.hardcoded.created").WhenSerializing(Field<Project>(p => p.Metadata["hardcoded"].Created));

			/** A cool feature here is that we'll evaluate variables passed to an indexer */
			var variable = "var";
			Expect("metadata.var").WhenSerializing(Field<Project>(p => p.Metadata[variable]));
			Expect("metadata.var.created").WhenSerializing(Field<Project>(p => p.Metadata[variable].Created));

			/**
			* If you are using Elasticearch's {ref_current}/_multi_fields.html[multi_fields], which you really should as they allow
			* you to analyze a string in a number of different ways, these __"virtual"__ sub fields
			* do not always map back on to your POCO. By calling `.Suffix()` on expressions, you describe the sub fields that
			* should be mapped and <<auto-map, how they are mapped>>
			*/
			Expect("leadDeveloper.firstName.raw").WhenSerializing(
				Field<Project>(p => p.LeadDeveloper.FirstName.Suffix("raw")));

			Expect("curatedTags.raw").WhenSerializing(
				Field<Project>(p => p.CuratedTags[0].Suffix("raw")));

			Expect("curatedTags.raw").WhenSerializing(
				Field<Project>(p => p.CuratedTags.First().Suffix("raw")));

			Expect("curatedTags.added.raw").WhenSerializing(
				Field<Project>(p => p.CuratedTags[0].Added.Suffix("raw")));

			Expect("metadata.hardcoded.raw").WhenSerializing(
				Field<Project>(p => p.Metadata["hardcoded"].Suffix("raw")));

			Expect("metadata.hardcoded.created.raw").WhenSerializing(
				Field<Project>(p => p.Metadata["hardcoded"].Created.Suffix("raw")));

			/**
			* You can even chain `.Suffix()` calls to any depth!
			*/
			Expect("curatedTags.name.raw.evendeeper").WhenSerializing(
				Field<Project>(p => p.CuratedTags.First().Name.Suffix("raw").Suffix("evendeeper")));

			/** Variables passed to suffix will be evaluated as well */
			var suffix = "unanalyzed";
			Expect("metadata.var.unanalyzed").WhenSerializing(
				Field<Project>(p => p.Metadata[variable].Suffix(suffix)));

			Expect("metadata.var.created.unanalyzed").WhenSerializing(
				Field<Project>(p => p.Metadata[variable].Created.Suffix(suffix)));
		}

		/**
		* Suffixes can also be appended to expressions using `.AppendSuffix()`. This is useful in cases where you want to apply the same suffix
		* to a list of fields.
		*/
		[U]
		public void AppendingSuffixToExpressions()
		{
			/** Here we have a list of expressions */
			var expressions = new List<Expression<Func<Project, object>>>
			{
				p => p.Name,
				p => p.Description,
				p => p.CuratedTags.First().Name,
				p => p.LeadDeveloper.FirstName
			};

			/** and we want to append the suffix "raw" to each */
			var fieldExpressions =
				expressions.Select<Expression<Func<Project, object>>, Field>(e => e.AppendSuffix("raw")).ToList();

			Expect("name.raw").WhenSerializing(fieldExpressions[0]);
			Expect("description.raw").WhenSerializing(fieldExpressions[1]);
			Expect("curatedTags.name.raw").WhenSerializing(fieldExpressions[2]);
			Expect("leadDeveloper.firstName.raw").WhenSerializing(fieldExpressions[3]);
		}

		/**=== Attribute based naming
		*
		* Using NEST's property attributes you can specify a new name for the properties
		*/
		public class BuiltIn
		{
			[Text(Name = "naam")]
			public string Name { get; set; }
		}
		[U]
		public void BuiltInAnnotiatons()
		{
			Expect("naam").WhenSerializing(Field<BuiltIn>(p => p.Name));
		}

		/**
		* Starting with NEST 2.x, we also ask the serializer if it can resolve a property to a name.
		* Here we ask the default `JsonNetSerializer` to resolve a property name and it takes
		* the `JsonPropertyAttribute` into account
		*/
		public class SerializerSpecific
		{
			[JsonProperty("nameInJson")]
			public string Name { get; set; }
		}
		[U]
		public void SerializerSpecificAnnotations()
		{
			Expect("nameInJson").WhenSerializing(Field<SerializerSpecific>(p => p.Name));
		}

		/**
		* If both a NEST property attribute and a serializer specific attribute are present on a property,
		* **NEST attributes take precedence**
		*/
		public class Both
		{
			[Text(Name = "naam")]
			[JsonProperty("nameInJson")]
			public string Name { get; set; }
		}
		[U]
		public void NestAttributeTakesPrecedence()
		{
			Expect("naam").WhenSerializing(Field<Both>(p => p.Name));
			Expect(new
			{
				naam = "Martijn Laarman"
			}).WhenSerializing(new Both { Name = "Martijn Laarman" });
		}


		/**[[field-inference-caching]]
		*=== Field Inference Caching
		*
		* Resolution of field names is cached _per_ `ConnectionSettings` instance. To demonstrate,
		* take the following simple POCOs
		*/
		class A { public C C { get; set; } }
		class B { public C C { get; set; } }
		class C
		{
			public string Name { get; set; }
		}

		[U]
		public void ExpressionsAreCachedButSeeDifferentTypes()
		{
			var connectionSettings = TestClient.CreateSettings(forceInMemory: true);
			var client = new ElasticClient(connectionSettings);

			var fieldNameOnA = client.Infer.Field(Field<A>(p => p.C.Name));
			var fieldNameOnB = client.Infer.Field(Field<B>(p => p.C.Name));

			/**
			* Here we have two similarly shaped expressions, one coming from A and one from B
			* that will resolve to the same field name, as expected
			*/

			fieldNameOnA.Should().Be("c.name");
			fieldNameOnB.Should().Be("c.name");

			/**
			* now we create a new connection settings with a re-map for `C` on class `A` to `"d"`
			* now when we resolve the field path for property `C` on `A`, it will be different than
			* for property `C` on `B`
			*/
			var newConnectionSettings = TestClient.CreateSettings(forceInMemory: true, modifySettings: s => s
				.InferMappingFor<A>(m => m
					.Rename(p => p.C, "d")
				)
			);
			var newClient = new ElasticClient(newConnectionSettings);

			fieldNameOnA = newClient.Infer.Field(Field<A>(p => p.C.Name));
			fieldNameOnB = newClient.Infer.Field(Field<B>(p => p.C.Name));

			fieldNameOnA.Should().Be("d.name");
			fieldNameOnB.Should().Be("c.name");

			/** however we didn't break inference on the first client instance using its separate connection settings */
			fieldNameOnA = client.Infer.Field(Field<A>(p => p.C.Name));
			fieldNameOnB = client.Infer.Field(Field<B>(p => p.C.Name));

			fieldNameOnA.Should().Be("c.name");
			fieldNameOnB.Should().Be("c.name");
		}

		/**[[field-inference-precedence]]
		*=== Inference Precedence
		* To wrap up, the precedence in which field names are inferred is:
		*
		* . A hard rename of the property on connection settings using `.Rename()`
		* . A NEST property mapping
		* . Ask the serializer if the property has a verbatim value e.g it has an explicit JsonProperty attribute.
		* . Pass the MemberInfo's Name to the DefaultFieldNameInferrer which by default camelCases
		*
		* The following example class will demonstrate this precedence
		*/
		class Precedence
		{
			[Text(Name = "renamedIgnoresNest")]
			[JsonProperty("renamedIgnoresJsonProperty")]
			public string RenamedOnConnectionSettings { get; set; } //<1> Even though this property has a NEST property mapping _and_ a `JsonProperty` attribute, We are going to provide a hard rename for it on ConnectionSettings later that should win.

			[Text(Name = "nestAtt")]
			[JsonProperty("jsonProp")]
			public string NestAttribute { get; set; } //<2> This property has both a NEST attribute and a `JsonProperty`, NEST should win.

			[JsonProperty("jsonProp")]
			public string JsonProperty { get; set; } //<3>  We should take the json property into account by itself

			[JsonProperty("dontaskme")]
			public string AskSerializer { get; set; } //<4> This property we are going to special case in our custom serializer to resolve to ask

			public string DefaultFieldNameInferrer { get; set; } //<5>  We are going to register a DefaultFieldNameInferrer on ConnectionSettings that will uppercase all properties.
		}

		/**
		* Here we create a custom serializer that renames any property named `AskSerializer` to `ask`
		*/
		class CustomSerializer : JsonNetSerializer
		{
			public CustomSerializer(IConnectionSettingsValues settings) : base(settings) { }

			public override IPropertyMapping CreatePropertyMapping(MemberInfo memberInfo)
			{
				return memberInfo.Name == nameof(Precedence.AskSerializer)
					? new PropertyMapping { Name = "ask" }
					: base.CreatePropertyMapping(memberInfo);
			}
		}

		[U]
		public void PrecedenceIsAsExpected()
		{
			/** here we provide an explicit rename of a property on `ConnectionSettings` using `.Rename()`
			* and all properties that are not mapped verbatim should be uppercased
			*/
			var usingSettings = WithConnectionSettings(s => s

				.InferMappingFor<Precedence>(m => m
					.Rename(p => p.RenamedOnConnectionSettings, "renamed")
				)
				.DefaultFieldNameInferrer(p => p.ToUpperInvariant())
			).WithSerializer(s => new CustomSerializer(s));

			usingSettings.Expect("renamed").ForField(Field<Precedence>(p => p.RenamedOnConnectionSettings));
			usingSettings.Expect("nestAtt").ForField(Field<Precedence>(p => p.NestAttribute));
			usingSettings.Expect("jsonProp").ForField(Field<Precedence>(p => p.JsonProperty));
			usingSettings.Expect("ask").ForField(Field<Precedence>(p => p.AskSerializer));
			usingSettings.Expect("DEFAULTFIELDNAMEINFERRER").ForField(Field<Precedence>(p => p.DefaultFieldNameInferrer));

			/** The same naming rules also apply when indexing a document */
			usingSettings.Expect(new []
			{
				"ask",
				"DEFAULTFIELDNAMEINFERRER",
				"jsonProp",
				"nestAtt",
				"renamed"
			}).AsPropertiesOf(new Precedence
			{
				RenamedOnConnectionSettings = "renamed on connection settings",
				NestAttribute = "using a nest attribute",
				JsonProperty = "the default serializer resolves json property attributes",
				AskSerializer = "serializer fiddled with this one",
				DefaultFieldNameInferrer = "shouting much?"
			});

		}
	}
}