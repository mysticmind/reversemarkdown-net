using ReverseMarkdown.Converters;
using ReverseMarkdown.Test.Children;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;


namespace ReverseMarkdown.Test {
    public class ChildConverterTests {
        [Fact]
        public void WhenConverter_A_IsReplacedByConverter_IgnoreAWhenHasClass()
        {
            var converter = new ReverseMarkdown.Converter(new Config(), typeof(IgnoreAWhenHasClass).Assembly);

            var type = converter.GetType();
            var prop = type.GetField("Converters", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(prop);

            var propValRaw = prop.GetValue(converter);

            Assert.NotNull(propValRaw);

            var propVal = (IDictionary<string, IConverter>) propValRaw;

            Assert.NotNull(propVal);

            var converters = propVal.Select(e => e.Value.GetType()).ToArray();

            Assert.DoesNotContain(typeof(A), converters);
            Assert.Contains(typeof(IgnoreAWhenHasClass), converters);
        }
    }
}
