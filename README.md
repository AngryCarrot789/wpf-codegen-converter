# WPFCodeGenConverter
Uses a C# compiler to generate a WPF converter from a script, to save having to create a class for each converter

This is purely just a demo of how to do it. But the added hassle of having to use special character codes like `&quot;` instead of `"` and not 
being able to use `using` statements (and having to directly access all types like `System.Windows.Visibility` directly)
almost make it not as convenient to use.

Though 1 good usage I guess could be an invert bool converter, but you could just make a singleton bool inverter converter that you use across your
entire project

# DynamicCodeConverter
To use it in XAML, you just set a binding's converter to `Converter={c:DynamicCodeConverter}`. The class extends `MarkdownExtension`, which means that you 
don't need to provide a specific instance; it's automatically created by XAML and also the class itself (via the `ProvideValue` method)

And then you provide your code in the `ConverterParameter` part of the binding. If the `ConverterParameter` is the last part of the binding expression, you can
encapsulate the script in single quotes (`'like this'`), and XAML will tolerate it

Currently, you can't convert back from the value. I was thinking of maybe using a TypeConverter or something along those lines to
allow more specific properties to be set... but then you lose the luxury of binding AFAIK. I couldn't get binding to work outside of the built in
`Binding`
