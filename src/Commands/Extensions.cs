using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public static class Extensions
{
    public static ICommandConfigurator AddCommand<TCommand>(this IConfigurator configurator)
        where TCommand : class, ICommand
    {
        var name = typeof(TCommand).Name.Replace("Command", "").ToLowerInvariant();
        return configurator.AddCommand<TCommand>(name);
    }

    public static Table AsTable<T>(this IEnumerable<T> items)
    {
        var table = new Table();
        var props = TypeDescriptor.GetProperties(typeof(T)).Cast<PropertyDescriptor>().ToList();

        foreach (var prop in props)
        {
            var name = prop.DisplayName;
            if (!name.Contains(' '))
            {
                // Separate words by upper case letters
                var sb = new StringBuilder();
                foreach (var c in name)
                {
                    if (char.IsUpper(c))
                        sb.Append(' ');
                    sb.Append(c);
                }
                name = sb.ToString().Trim();
            }

            Action<TableColumn>? configure = null;

            if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateOnly))
                configure = c => c.Centered();

            if (prop.PropertyType == typeof(bool))
                configure = c => c.Centered();

            if (prop.PropertyType == typeof(int))
                configure = c => c.RightAligned();

            table.AddColumn(name, configure);
        }

        var values = new List<string>();
        foreach (var item in items)
        {
            values.Clear();
            foreach (var prop in props)
            {
                var value = prop.GetValue(item);
                if (value is DateTime dt)
                    values.Add(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                else if (value is DateOnly date)
                    values.Add(date.ToString("yyyy-MM-dd"));
                else if (value is bool b)
                    if (b) values.Add("[green]✔[/]");
                    else values.Add("");
                else
                    values.Add(value?.ToString() ?? "");
            }
            table.AddRow(values.ToArray());
        }

        return table;
    }
}
