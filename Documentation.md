# Remove and Sort Usings

The Remove and Sort Usings feature is a tool to maintain organization of `using` (C#) and `Imports` (Visual Basic) statements. These statements can easily become large disordered walls of text at the top of source code files. As source code undergoes development, sometimes namespaces that were previously imported are no longer needed, and new namespaces get added in an unpredictable order.

When the Remove and Sort feature is activated for a source code file, it considers all of the namespace import statements, removes those that aren't actually being used by code, and sorts the remainder alphabetically.

## Customization

The Remove and Sort feature can work in multiple passes. Each pass selects a subset of the namespace import statements to process. By default, the feature processes all statements in one pass, but the `dotnet_import_directives_custom_order` setting in `.editorconfig` can be used to configure the groups into which the statements should be broken. This allows you to specify that more important namespaces appear in a preferred location, such as closer to the top of the list. Other namespaces then get placed afterward, sorted independently.

To control the order in this way, the `dotnet_import_directives_custom_order` takes a series of directives, separated by `;`, which indicate which import statements should be included in the pass. These are processed from left to right, and an import statement will be included in the first group by which it is matched.

In its simplest form, each directive identifies a namespace to be included. For instance, with a configuration of `C;B;A`, `using` statements would get sorted as:

```
using C.First;
using C.Second;
using B.First;
using B.Second;
using B.Second.Tests;
using A.First;
using A.Second;
using A.Third;
```

If the order is changed in the configuration, such as `C;A;B`, then the output changes correspondingly:

```
using C.First;
using C.Second;
using A.First;
using A.Second;
using A.Third;
using B.First;
using B.Second;
using B.Second.Tests;
```

If, after the last pass, there are still import statements that have not been processed, then all remaining statements are processed and sorted as a final pass.

In addition, if the `dotnet_separate_import_directive_groups` option is enabled, each pass is separated from the next by a blank line:

```
using C.First;
using C.Second;

using A.First;
using A.Second;
using A.Third;

using B.First;
using B.Second;
using B.Second.Tests;
```

Finally, there are two ways to fine-tune how this grouping is done.

* If you want _less_ grouping, then specifying a final pass of `*` will cause all remaining import statements to be formatted in a single block. With `dotnet_separate_import_directive_groups` enabled, the default is that they are automatically separated with a blank line between each unique top-level namespace.

* If you want _more_ grouping, then it is possible to indicate namespaces that should be treated as their own "root" namespaces, for the purpose of separating statements into groups. For instance, if you have import statements for multiple features by the same company, maybe you'd like each feature to be its own group, even though they all are in the `Company` namespace. This is specified by using `*(List,Of,Namespaces)` or `**(List,Of,Namespaces)`, and it is possible to also include any remaining namespaces in the same sorting pass using `**(List,Of,Namespaces,*)`.

    * If the directive starts with `*`, then the pass is treated as a single block, though is still separated from the preceding and following passes.
    * If the directive starts with `**`, then each matching namespace is treated as a "root" namespace, and blocks are formed for the child namespaces within.

For instance, provided that `dotnet_separate_import_directive_groups` is also enabled, the formatted list of namespaces could be like this with `System;*`:

```
using System;
using System.IO;

using AutoMapper;
using Company.FeatureOne;
using Company.FeatureOne.Implementation;
using Company.FeatureTwo;
using Company.FeatureTwo.Contracts;
using Company.FeatureTwo.Model;
using First;
using Second;
using Second.Contracts;
using Second.Model;
using Third;
using Third.Implementation;
```

Or like this with `System;**`:

```
using System;
using System.IO;

using AutoMapper;

using Company.FeatureOne;
using Company.FeatureOne.Implementation;
using Company.FeatureTwo;
using Company.FeatureTwo.Contracts;
using Company.FeatureTwo.Model;

using First;

using Second;
using Second.Contracts;
using Second.Model;

using Third;
using Third.Implementation;
```

Or like this with `System;*(Company)` (the same as `System;*(Company);**`):

```
using System;
using System.IO;

using Company.FeatureOne;
using Company.FeatureOne.Implementation;
using Company.FeatureTwo;
using Company.FeatureTwo.Contracts;
using Company.FeatureTwo.Model;

using AutoMapper;

using First;

using Second;
using Second.Contracts;
using Second.Model;

using Third;
using Third.Implementation;
```

Or like this with `System;**(Company,*)` (note that `Company` is now sorted amongst the others, coming after `AutoMapper`, and `Company.FeatureOne` is separated from `Company.FeatureTwo`):

```
using System;
using System.IO;

using AutoMapper;

using Company.FeatureOne;
using Company.FeatureOne.Implementation;

using Company.FeatureTwo;
using Company.FeatureTwo.Contracts;
using Company.FeatureTwo.Model;

using First;

using Second;
using Second.Contracts;
using Second.Model;

using Third;
using Third.Implementation;
```

## Legacy Configuration

In earlier specifications, there is an additional .editorconfig option that is related to this functionality, but does not allow as much flexibility in the specification.

The `dotnet_sort_system_directives_first` can be used to ensure that all `System` namespaces appear at the top of the list, irrespective of other sorting. This is equivalent to a value of `System,**` for `dotnet_import_directives_custom_order`.

If `dotnet_import_directives_custom_order` is assigned a value, then this legacy option is overridden and ignored.
