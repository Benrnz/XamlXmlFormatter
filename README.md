XamlXmlFormatter
================

This simple utility is intended to format Xaml and Xml files how I prefer them. Such that there is no horizontal scrolling
and all the attributes are sorted in alphabetical order.
For example:
        &lt;Border x:Name="MyBorder"
                Background="{StaticResource BgColour}"
                Grid.RowSpan="2"
                HorizontalAlignment="Stretch"
                Margin="5"
                VerticalAlignment="Stretch" /&gt;

You can use it as a command line utility or as a tool inside Visual Studio.  

To use with Visual Studio, add a new External Tool, with the command pointing to the XamlFormatter.exe with the argument
$(ItemPath).
