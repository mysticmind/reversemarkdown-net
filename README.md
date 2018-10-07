# Meet ReverseMarkdown

[![Windows Build status](https://ci.appveyor.com/api/projects/status/xse0bia9olr5shxr?svg=true)](https://ci.appveyor.com/project/BabuAnnamalai/reversemarkdown-net) [![Windows Build status](https://api.travis-ci.org/mysticmind/reversemarkdown-net.svg)](https://travis-ci.org/mysticmind/reversemarkdown-net) [![NuGet Version](https://badgen.net/nuget/v/reversemarkdown)](https://www.nuget.org/packages/ReverseMarkdown/)

ReverseMarkdown is a Html to Markdown (http://daringfireball.net/projects/markdown/syntax) converter library in C#. Conversion is very reliable since HtmlAgilityPack (HAP) library is used for traversing the Html DOM.

Note that the library implementation is based on the Ruby based Html to Markdown converter [ xijo/reverse_markdown](https://github.com/xijo/reverse_markdown).

## Usage

You can install the package from NuGet using `Install-Package ReverseMarkdown` or clone the repository and built it yourself. 

```csharp
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);

//result This a sample **paragraph** from [my site](http://test.com)
```

```csharp
// with config
bool githubFlavored = true; // generate GitHub flasvoured markdown, supported for BR, PRE and table tags
bool removeComments = true; // will ignore all comments
var config = new ReverseMarkdown.Config(UnknownTagsOption.PassThrough, 
                githubFlavoured:githubFlavoured, removeComments:removeComments);
var converter = new ReverseMarkdown.Converter(config);
```

### UnknownTags config - how to handle unknown tags. 
Valid options are:
* `UnknownTagsOption.PassThrough` - Include the unknown tag completely into the result. This is the default
* `UnknownTagsOption.Drop` - Drop the unknown tag and its content
* `UnknownTagsOption.Bypass` - Ignore the unknown tag but try to convert its content
* `UnknownTagsOption.Raise` - Raise an error to let you know

> Note that UnknownTags config has been changed to an enumeration in v2.0.0 (breaking change)

## Features
* Supports all the established html tags like h1, h2, h3, h4, h5, h6, p, em, strong, i, b, blockquote, code, img, a, hr, li, ol, ul, table, tr, th, td, br
* Can deal with nested lists
* Github Flavoured Markdown conversion supported for br, pre and table. Use `var config = new ReverseMarkdown.Config(githubFlavoured:true);`. By default table will always be converted to Github flavored markdown immaterial of this flag.

## Copyright

Copyright © 2017 Babu Annamalai

## License

ReverseMarkdown is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to [License file](https://github.com/mysticmind/reversemarkdown-net/blob/master/LICENSE) for more information.
