namespace OreoLeads.Domain.Enums;

public enum DateRangePreset { Today, Yesterday, Last7Days, Last30Days, Last90Days, ThisYear, Custom }
public enum WidgetType { KpiCard, LineChart, AreaChart, BarChart, PieChart, DonutChart, FunnelChart, Table, Heatmap }
public enum ReportFrequency { Daily, Weekly, Monthly }
public enum ReportFormat { Csv, Excel, Pdf }
public enum ReportStatus { Pending, Running, Completed, Failed }
public enum ForecastMethod { LinearTrend, MovingAverage }
