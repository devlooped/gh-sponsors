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
        var props = typeof(T).GetProperties();

        foreach (var prop in props)
        {
            table.AddColumn(prop.Name);
            if (prop.PropertyType == typeof(DateTime))
                table.Columns[table.Columns.Count - 1].RightAligned();

            if (prop.PropertyType == typeof(int))
                table.Columns[table.Columns.Count - 1].RightAligned();
        }

        foreach (var item in items)
        {
            table.AddRow(props.Select(x => x.GetValue(item)?.ToString() ?? "").ToArray());
        }

        return table;
    }
}
