﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System.Composition;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.Convention;
using System.Composition.UnitTests.Util;
using System.Composition.Runtime;
using Microsoft.Composition.Demos.ExtendedCollectionImports;
using Microsoft.Composition.Demos.ExtendedCollectionImports.Dictionaries;

namespace System.Composition.UnitTests
{
    public interface IValued { }

    [Export(typeof(IValued)), ExportMetadata("Value", "A")]
    public class ValueA : IValued { }

    [Export(typeof(IValued)), ExportMetadata("Value", "B")]
    public class ValueB : IValued { }

    [Export(typeof(IValued))]
    public class ValueMissing : IValued { }

    [Export(typeof(IValued)), ExportMetadata("Value", 1)]
    public class NonStringValue : IValued { }

    [Export]
    public class Consumer
    {
        [Import, KeyByMetadata("Value")]
        public IDictionary<string, IValued> Values { get; set; }
    }

    [Export]
    public class LazyConsumer
    {
        [Import, KeyByMetadata("Value")]
        public IDictionary<string, Lazy<IValued>> Values { get; set; }
    }

    public class ConventionConsumer
    {
        public IDictionary<string, IValued> Values;

        public ConventionConsumer(
            [KeyByMetadata("Value")] IDictionary<string, IValued> values)
        {
            Values = values;
        }
    }

    [TestClass]
    public class DictionaryImportTests
    {
        CompositionContext CreateContainer(params Type[] types)
        {
            var configuration = new ContainerConfiguration()
                .WithParts(types)
                .WithProvider(new DictionaryExportDescriptorProvider());

            return configuration.CreateContainer();
        }

        [TestMethod]
        public void DictionaryImportsKeyedByMetadata()
        {
            var container = CreateContainer(new[] { typeof(ValueA), typeof(ValueB), typeof(Consumer) });

            var consumer = container.GetExport<Consumer>();

            Assert.IsInstanceOfType(consumer.Values["A"], typeof(ValueA));
            Assert.IsInstanceOfType(consumer.Values["B"], typeof(ValueB));
            Assert.AreEqual(2, consumer.Values.Count());
        }

        [TestMethod]
        public void DictionaryImportsRecieveMetadataFromNestedAdapters()
        {
            var container = CreateContainer(new[] { typeof(ValueA), typeof(ValueB), typeof(LazyConsumer) });

            var consumer = container.GetExport<LazyConsumer>();

            var a = (Lazy<IValued>)consumer.Values["A"];
            Assert.IsFalse(a.IsValueCreated);
        }

        [TestMethod]
        public void WhenAMetadataKeyIsDuplicatedAnInformativeExceptionIsThrown()
        {
            var container = CreateContainer(typeof(ValueA), typeof(ValueA), typeof(Consumer));
            var x = AssertX.Throws<CompositionFailedException>(() => container.GetExport<Consumer>());
            Assert.AreEqual("The metadata 'Value' cannot be used as a dictionary import key because the value 'A' is associated with exports from parts 'ValueA' and 'ValueA'.", x.Message);
        }

        [TestMethod]
        public void WhenAMetadataKeyIsMissingAnInformativeExceptionIsThrown()
        {
            var container = CreateContainer(typeof(ValueA), typeof(ValueMissing), typeof(Consumer));
            var x = AssertX.Throws<CompositionFailedException>(() => container.GetExport<Consumer>());
            Assert.AreEqual("The metadata 'Value' cannot be used as a dictionary import key because it is missing from exports on part(s) 'ValueMissing'.", x.Message);
        }

        [TestMethod]
        public void WhenAMetadataValueIsOfTheWrongTypeAnInformativeExceptionIsThrown()
        {
            var container = CreateContainer(typeof(ValueA), typeof(NonStringValue), typeof(Consumer));
            var x = AssertX.Throws<CompositionFailedException>(() => container.GetExport<Consumer>());
            Assert.AreEqual("The metadata 'Value' cannot be used as a dictionary import key of type 'String' because the value(s) supplied by 'NonStringValue' are of the wrong type.", x.Message);
        }

        [TestMethod]
        public void DictionaryImportsCompatibleWithConventionBuilder()
        {
            var rb = new ConventionBuilder();
            rb.ForType<ConventionConsumer>().Export();
            var container = new ContainerConfiguration()
                .WithPart<ConventionConsumer>(rb)
                .WithParts(typeof(ValueA), typeof(ValueB))
                .WithProvider(new DictionaryExportDescriptorProvider())
                .CreateContainer();

            var consumer = container.GetExport<ConventionConsumer>();

            Assert.AreEqual(2, consumer.Values.Count());
        }
    }
}
