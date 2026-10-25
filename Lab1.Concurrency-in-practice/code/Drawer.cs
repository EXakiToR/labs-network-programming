using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// A standalone file-based app, launched by Program.cs with the measurements on stdin.
public static partial class Drawer
{
    private sealed record ThreadMetric(int Threads, long ElapsedMs, long PeakMemoryBytes);
    private sealed record WorkerMetric(int Workers, long ElapsedMs, long PeakMemoryBytes);
    private sealed record SignMetric(
        string Version, int Run, int Workers, long ElapsedMs,
        long Negative, long Positive, long Zero);
    private sealed record Results(
        ThreadMetric[] TaskOneA, SignMetric[] TaskOneB,
        ThreadMetric[] TaskOneC, WorkerMetric[] TaskTwo);
    private sealed record Series(string Name, string Color, double[] Values);

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(Results))]
    private partial class ResultsJsonContext : JsonSerializerContext { }

    public static int Main()
    {
        try
        {
            var results = JsonSerializer.Deserialize(Console.In.ReadToEnd(), ResultsJsonContext.Default.Results)
                ?? throw new InvalidDataException("No measurements were supplied.");

            string directory = Path.Combine(Path.GetTempPath(), "Lab1-results");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"results-{Guid.NewGuid():N}.html");
            File.WriteAllText(path, BuildReport(results), Encoding.UTF8);

            Console.WriteLine($"Results dashboard: {path}");
            if (OperatingSystem.IsMacOS())
            {
                // An .html file may be associated with an editor; Safari is built into macOS.
                var startInfo = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false };
                startInfo.ArgumentList.Add("-a");
                startInfo.ArgumentList.Add("Safari");
                startInfo.ArgumentList.Add(path);
                using var launcher = Process.Start(startInfo)
                    ?? throw new IOException("Could not start the browser.");
                launcher.WaitForExit();
                if (launcher.ExitCode != 0)
                    throw new IOException($"The browser could not open the report. Open this file manually: {path}");
            }
            else
            {
                using var browser = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not open the results dashboard: {exception.Message}");
            return 1;
        }
    }

    private static string BuildReport(Results results)
    {
        int count = results.TaskOneA.Length + results.TaskOneB.Length
            + results.TaskOneC.Length + results.TaskTwo.Length;
        var html = new StringBuilder();
        html.Append("""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Lab 1 · Concurrency results</title>
            <style>
            :root { color-scheme: light; font-family: system-ui, sans-serif; color: #17283d; background: #f2f5fa; }
            * { box-sizing: border-box; }
            body { margin: 0; }
            main { max-width: 1600px; margin: auto; padding: 38px 28px 60px; }
            h1 { font-size: clamp(28px, 4vw, 42px); margin: 6px 0 10px; letter-spacing: -.035em; }
            h2 { font-size: 23px; margin: 0 0 8px; }
            h3 { font-size: 16px; margin: 0; }
            p { line-height: 1.6; }
            .eyebrow { color: #435c7b; font-size: 12px; text-transform: uppercase; letter-spacing: .14em; font-weight: 700; }
            .muted { color: #52647b; }
            nav { display: flex; flex-wrap: wrap; gap: 10px; margin: 24px 0 32px; }
            nav a { color: #244a9b; background: white; border: 1px solid #dce3ed; border-radius: 9px; padding: 10px 17px; text-decoration: none; }
            section { margin: 0 0 34px; scroll-margin-top: 20px; }
            .charts { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; }
            .card, details, .empty { background: white; border: 1px solid #dce3ed; border-radius: 14px; padding: 22px; }
            .chart-scroll, .table-scroll { overflow-x: auto; }
            svg { display: block; width: 100%; min-width: 570px; height: auto; margin-top: 12px; }
            svg text { font: 12px system-ui, sans-serif; fill: #52647b; }
            svg .point:hover, svg rect.bar:hover { stroke: #17283d; stroke-width: 2; }
            .legend { display: flex; flex-wrap: wrap; gap: 16px; margin-top: 12px; font-size: 13px; }
            .legend span { display: inline-flex; gap: 7px; align-items: center; }
            .swatch { width: 10px; height: 10px; border-radius: 3px; display: inline-block; }
            .run-filters { border: 1px solid #dce3ed; border-radius: 14px; padding: 16px 20px; margin: 0 0 18px; background: white; }
            .run-filters legend { padding: 0 6px; font-weight: 600; }
            .run-options, .filter-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; }
            .run-options label { display: inline-flex; align-items: center; gap: 8px; padding: 9px 12px; border: 1px solid #dce3ed; border-radius: 8px; cursor: pointer; font-size: 14px; }
            .run-options label:has(input:not(:checked)) { color: #52647b; background: #f2f5fa; }
            .run-options input { accent-color: #3565d9; width: 16px; height: 16px; }
            .filter-actions { margin-top: 14px; font-size: 13px; }
            .filter-actions button { background: #f2f5fa; color: #244a9b; border: 1px solid #dce3ed; border-radius: 7px; padding: 7px 11px; cursor: pointer; font: inherit; }
            details { margin-top: 16px; padding: 16px 22px; }
            summary { cursor: pointer; font-weight: 600; }
            table { width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 14px; font-variant-numeric: tabular-nums; }
            th, td { padding: 11px 14px; text-align: right; white-space: nowrap; border-bottom: 1px solid #e5eaf1; }
            th { color: #52647b; font-weight: 600; }
            th:first-child, td:first-child { text-align: left; }
            tbody tr:hover { background: #f2f5fa; }
            .empty { color: #52647b; }
            footer { color: #52647b; font-size: 13px; }
            @media (max-width: 1000px) { .charts { grid-template-columns: 1fr; } main { padding: 24px 16px; } }
            @media print { body { background: white; } nav { display: none; } .charts { grid-template-columns: 1fr; } .card { break-inside: avoid; } svg { min-width: 0; } }
            </style>
            </head>
            <body><main>
            <div class="eyebrow">Lab 1 / Concurrency in practice</div>
            <h1>Execution results</h1>
            <p class="muted">Compare execution time, peak memory, and sign counts. Hover over a point or bar for its value;
            expand a table to inspect every measurement. All charts start at zero.</p>
            """);
        html.Append($"<p class=\"muted\">{count} measurements · Snapshot taken {H(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</p>");
        html.Append("<nav><a href=\"#task1a\">Task 1A</a><a href=\"#task1b\">Task 1B</a><a href=\"#task1c\">Task 1C</a><a href=\"#task2\">Task 2</a></nav>");

        AppendWorkerResults(html, "task1a", "Task 1A", "Threads",
            results.TaskOneA.Select(m => new WorkerMetric(m.Threads, m.ElapsedMs, m.PeakMemoryBytes)).ToArray());
        AppendSignResults(html, results.TaskOneB);
        AppendWorkerResults(html, "task1c", "Task 1C", "Threads",
            results.TaskOneC.Select(m => new WorkerMetric(m.Threads, m.ElapsedMs, m.PeakMemoryBytes)).ToArray());
        AppendWorkerResults(html, "task2", "Task 2", "Workers", results.TaskTwo);

        html.Append("<footer>Memory uses MiB (1,048,576 bytes). Tables retain the original byte counts. "
            + "This is a saved snapshot; choose d again after collecting more results.</footer></main></body></html>");
        return html.ToString();
    }

    private static void AppendWorkerResults(
        StringBuilder html, string id, string title, string workerLabel, WorkerMetric[] metrics)
    {
        html.Append($"<section id=\"{id}\"><h2>{H(title)}</h2>");
        if (metrics.Length == 0)
        {
            html.Append("<p class=\"empty\">No measurements yet. Run this task, then choose d again.</p></section>");
            return;
        }

        // Keep all original measurements in the table, and average repeated worker settings in the graphs.
        var groups = metrics.GroupBy(m => m.Workers).OrderBy(g => g.Key).ToArray();
        string[] labels = groups.Select(g => g.Key.ToString(CultureInfo.InvariantCulture)).ToArray();
        html.Append($"<p class=\"muted\">{metrics.Length} measurements. Lower time and memory are better. "
            + "Graphs show the mean when a setting has multiple measurements; settings are equally spaced.</p><div class=\"charts\">");
        AppendChart(html, "Execution time", "ms", workerLabel, labels,
            new[] { new Series("Elapsed time", "#3565d9", groups.Select(g => g.Average(m => (double)m.ElapsedMs)).ToArray()) });
        AppendChart(html, "Peak memory", "MiB", workerLabel, labels,
            new[] { new Series("Peak memory", "#128277", groups.Select(g => g.Average(m => m.PeakMemoryBytes / 1048576.0)).ToArray()) });
        html.Append("</div>");
        AppendTable(html, new[] { "Measurement", workerLabel, "Elapsed (ms)", "Peak memory (bytes)", "Peak memory (MiB)" },
            metrics.Select((m, i) => new[] { (i + 1).ToString(), m.Workers.ToString(),
                m.ElapsedMs.ToString(), m.PeakMemoryBytes.ToString(), Format(m.PeakMemoryBytes / 1048576.0) }));
        html.Append("</section>");
    }

    private static void AppendSignResults(StringBuilder html, SignMetric[] metrics)
    {
        html.Append("<section id=\"task1b\"><h2>Task 1B</h2>");
        if (metrics.Length == 0)
        {
            html.Append("<p class=\"empty\">No measurements yet. Run Task 1B, then choose d again.</p></section>");
            return;
        }

        html.Append("<p class=\"muted\">Compare each version and run. Differences in sign counts can reveal lost updates; "
            + "the table includes totals for checking consistency. Small zero counts may be hard to see beside the other counts; hover or use the table.</p>");
        html.Append("<fieldset class=\"run-filters\"><legend>Show runs</legend><div class=\"run-options\">");
        for (int i = 0; i < metrics.Length; i++)
        {
            SignMetric metric = metrics[i];
            html.Append($"<label><input type=\"checkbox\" data-run=\"{i}\" checked>"
                + $"{H(metric.Version)} · Run {metric.Run} · {metric.Workers} workers</label>");
        }
        html.Append("</div><div class=\"filter-actions\"><button type=\"button\" data-select-all>Select all</button>"
            + "<button type=\"button\" data-select-none>Clear selection</button>"
            + "<span class=\"muted\" data-run-status role=\"status\" aria-live=\"polite\"></span></div>"
            + "<p class=\"muted\">Uncheck a run to hide it from both graphs. Scales adjust automatically; the table keeps all measurements.</p></fieldset><div class=\"charts\">");
        string[] labels = metrics.Select(m => $"{m.Version}|Run {m.Run} · {m.Workers} workers").ToArray();
        AppendChart(html, "Execution time by version / run", "ms", "Version / run", labels,
            new[] { new Series("Elapsed time", "#3565d9", metrics.Select(m => (double)m.ElapsedMs).ToArray()) }, bars: true);
        AppendChart(html, "Sign counts by version / run", "numbers", "Version / run", labels,
            new[] {
                new Series("Negative", "#d46b36", metrics.Select(m => (double)m.Negative).ToArray()),
                new Series("Positive", "#128277", metrics.Select(m => (double)m.Positive).ToArray()),
                new Series("Zero", "#8554bb", metrics.Select(m => (double)m.Zero).ToArray())
            }, bars: true);
        html.Append("</div>");
        AppendTable(html, new[] { "Version", "Run", "Workers", "Elapsed (ms)", "Negative", "Positive", "Zero", "Total counted" },
            metrics.Select(m => new[] { m.Version, m.Run.ToString(), m.Workers.ToString(), m.ElapsedMs.ToString(),
                m.Negative.ToString(), m.Positive.ToString(), m.Zero.ToString(),
                (m.Negative + m.Positive + m.Zero).ToString() }));
        html.Append("""
            <script>
            (() => {
                const section = document.getElementById('task1b');
                const choices = [...section.querySelectorAll('input[data-run]')];
                const charts = [...section.querySelectorAll('svg')];
                const number = new Intl.NumberFormat('en-US', { maximumFractionDigits: 3 });
                const axisFormat = value => value >= 1000000 ? number.format(value / 1000000) + 'M'
                    : value >= 1000 ? number.format(value / 1000) + 'k' : number.format(value);

                for (const chart of charts) {
                    const empty = document.createElement('p');
                    empty.className = 'empty';
                    empty.textContent = 'No runs selected. Choose a run above to display it.';
                    empty.hidden = true;
                    chart.parentElement.after(empty);
                }

                function update() {
                    const selected = choices.filter(choice => choice.checked).map(choice => choice.dataset.run);
                    section.querySelector('[data-run-status]').textContent = `${selected.length} of ${choices.length} runs shown`;
                    for (const chart of charts) {
                        const container = chart.parentElement;
                        container.hidden = selected.length === 0;
                        container.nextElementSibling.hidden = selected.length !== 0;
                        if (selected.length === 0) continue;

                        const width = Math.max(780, selected.length * 155 + 110);
                        const left = 85, top = 30, plotHeight = 238;
                        const step = (width - left - 25) / selected.length;
                        const marks = [...chart.querySelectorAll('[data-value]')];
                        let max = 0;
                        for (const mark of marks) {
                            if (selected.includes(mark.dataset.run)) max = Math.max(max, Number(mark.dataset.value));
                        }
                        const ceiling = max > 0 ? max * 1.1 : 1;
                        const y = value => top + plotHeight * (1 - value / ceiling);
                        chart.setAttribute('viewBox', `0 0 ${width} 350`);
                        chart.style.minWidth = (width > 1000 ? width : 570) + 'px';
                        chart.querySelectorAll('[data-grid]').forEach(line => line.setAttribute('x2', width - 25));
                        chart.querySelectorAll('[data-tick]').forEach(label => {
                            label.textContent = axisFormat(ceiling * Number(label.dataset.tick) / 5);
                        });
                        chart.querySelector('[data-axis-title]').setAttribute('x', left + (width - left - 25) / 2);
                        chart.querySelectorAll('[data-label]').forEach(label => {
                            const index = selected.indexOf(label.dataset.run);
                            label.style.display = index < 0 ? 'none' : '';
                            if (index < 0) return;
                            const x = left + step * (index + 0.5);
                            label.setAttribute('x', x);
                            label.querySelectorAll('tspan').forEach(line => line.setAttribute('x', x));
                        });
                        for (const mark of marks) {
                            const index = selected.indexOf(mark.dataset.run);
                            mark.style.display = index < 0 ? 'none' : '';
                            if (index < 0) continue;
                            const value = Number(mark.dataset.value);
                            const barWidth = step * 0.72 / Number(chart.dataset.seriesCount);
                            const x = left + step * (index + 0.5) - step * 0.36 + Number(mark.dataset.series) * barWidth;
                            if (mark.tagName.toLowerCase() === 'rect') {
                                mark.setAttribute('x', x);
                                mark.setAttribute('y', y(value));
                                mark.setAttribute('width', barWidth - 2);
                                mark.setAttribute('height', top + plotHeight - y(value));
                            } else {
                                mark.setAttribute('cx', x + (barWidth - 2) / 2);
                                mark.setAttribute('cy', y(value));
                            }
                        }
                    }
                }

                choices.forEach(choice => choice.addEventListener('change', update));
                function selectAll(checked) {
                    choices.forEach(choice => { choice.checked = checked; });
                    update();
                }
                section.querySelector('[data-select-all]').addEventListener('click', () => selectAll(true));
                section.querySelector('[data-select-none]').addEventListener('click', () => selectAll(false));
                update();
            })();
            </script>
            </section>
            """);
    }

    private static void AppendChart(
        StringBuilder html, string title, string unit, string xTitle,
        string[] labels, Series[] series, bool bars = false)
    {
        double width = Math.Max(780, labels.Length * (bars ? 155 : 65) + 110);
        const double height = 350, left = 85, top = 30, bottom = 82;
        double plotWidth = width - left - 25;
        double plotHeight = height - top - bottom;
        double max = series.SelectMany(s => s.Values).DefaultIfEmpty(0).Max();
        double ceiling = max > 0 ? max * 1.1 : 1;
        double step = plotWidth / Math.Max(1, labels.Length);
        double X(int index) => left + step * (index + 0.5);
        double Y(double value) => top + plotHeight * (1 - value / ceiling);

        html.Append($"<article class=\"card\"><h3>{H(title)}</h3><div class=\"chart-scroll\">");
        double minimumWidth = width > 1000 ? width : 570;
        html.Append($"<svg viewBox=\"0 0 {N(width)} {N(height)}\" style=\"min-width:{N(minimumWidth)}px\" data-series-count=\"{series.Length}\" role=\"img\" aria-label=\"{H(title)}\">");
        html.Append($"<title>{H(title)} ({H(unit)})</title><text x=\"{N(left)}\" y=\"15\">{H(unit)}</text>");
        for (int tick = 0; tick <= 5; tick++)
        {
            double value = ceiling * tick / 5;
            double y = Y(value);
            html.Append($"<line data-grid x1=\"{N(left)}\" y1=\"{N(y)}\" x2=\"{N(width - 25)}\" y2=\"{N(y)}\" stroke=\"#e3e9f1\"/>");
            html.Append($"<text data-tick=\"{tick}\" x=\"{N(left - 12)}\" y=\"{N(y + 4)}\" text-anchor=\"end\">{H(AxisFormat(value))}</text>");
        }

        for (int i = 0; i < labels.Length; i++)
        {
            string[] lines = labels[i].Split('|');
            html.Append($"<text data-label data-run=\"{i}\" x=\"{N(X(i))}\" y=\"{N(top + plotHeight + 22)}\" text-anchor=\"middle\"><title>{H(labels[i].Replace('|', ' '))}</title>");
            for (int line = 0; line < lines.Length; line++)
                html.Append($"<tspan x=\"{N(X(i))}\" dy=\"{(line == 0 ? 0 : 17)}\">{H(lines[line])}</tspan>");
            html.Append("</text>");
        }

        for (int s = 0; s < series.Length; s++)
        {
            Series current = series[s];
            if (!bars)
            {
                string points = string.Join(" ", current.Values.Select((v, i) => $"{N(X(i))},{N(Y(v))}"));
                html.Append($"<polyline points=\"{points}\" fill=\"none\" stroke=\"{current.Color}\" stroke-width=\"3\"/>");
            }

            for (int i = 0; i < current.Values.Length; i++)
            {
                double value = current.Values[i];
                string tooltip = H($"{labels[i].Replace('|', ' ')} · {current.Name}: {Format(value)} {unit}");
                if (bars)
                {
                    double barWidth = step * 0.72 / series.Length;
                    double x = X(i) - step * 0.36 + s * barWidth;
                    string data = $"data-run=\"{i}\" data-series=\"{s}\" data-value=\"{N(value)}\"";
                    html.Append($"<rect class=\"bar\" {data} x=\"{N(x)}\" y=\"{N(Y(value))}\" width=\"{N(barWidth - 2)}\" height=\"{N(top + plotHeight - Y(value))}\" fill=\"{current.Color}\"><title>{tooltip}</title></rect>");
                    // Zero-height bars still need a visible, hoverable mark.
                    if (value == 0)
                        html.Append($"<circle class=\"point\" {data} cx=\"{N(x + (barWidth - 2) / 2)}\" cy=\"{N(Y(0))}\" r=\"4\" fill=\"{current.Color}\"><title>{tooltip}</title></circle>");
                }
                else
                {
                    html.Append($"<circle class=\"point\" cx=\"{N(X(i))}\" cy=\"{N(Y(value))}\" r=\"5\" fill=\"{current.Color}\"><title>{tooltip}</title></circle>");
                }
            }
        }

        html.Append($"<text data-axis-title x=\"{N(left + plotWidth / 2)}\" y=\"{N(height - 10)}\" text-anchor=\"middle\">{H(xTitle)}</text></svg></div><div class=\"legend\">");
        foreach (Series current in series)
            html.Append($"<span><i class=\"swatch\" style=\"background:{current.Color}\"></i>{H(current.Name)}</span>");
        html.Append("</div></article>");
    }

    private static void AppendTable(StringBuilder html, string[] headings, IEnumerable<string[]> rows)
    {
        html.Append("<details><summary>All measurements · exact values</summary><div class=\"table-scroll\"><table><thead><tr>");
        foreach (string heading in headings)
            html.Append($"<th scope=\"col\">{H(heading)}</th>");
        html.Append("</tr></thead><tbody>");
        foreach (string[] row in rows)
        {
            html.Append("<tr>");
            foreach (string cell in row)
                html.Append($"<td>{H(cell)}</td>");
            html.Append("</tr>");
        }
        html.Append("</tbody></table></div></details>");
    }

    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Format(double value) => value.ToString("#,0.###", CultureInfo.InvariantCulture);
    private static string AxisFormat(double value) => value >= 1_000_000 ? N(value / 1_000_000) + "M"
        : value >= 1_000 ? N(value / 1_000) + "k" : N(value);
}
