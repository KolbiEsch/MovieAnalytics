function DestroyOldChart(chartID) {
    let oldChart = Chart.getChart(chartID);
    if (oldChart != null) {
        oldChart.destroy();
    }
}

function getGradient(ctx, chartArea, indexAxis, colorsArr) {
    let gradient;
    if (indexAxis == "x") {
        gradient = ctx.createLinearGradient(chartArea.left, 0, chartArea.right, 0);
    } else {
        gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
    }
    const [color1, color2, color3] = colorsArr;
    gradient.addColorStop(0, color1);
    gradient.addColorStop(0.45, color2);
    gradient.addColorStop(1, color3);

    return gradient;
}

const chartBase = (Chart) => {
    Chart.defaults.plugins.tooltip.backgroundColor = "#445566";
    Chart.defaults.plugins.tooltip.displayColors = false;
    Chart.defaults.plugins.tooltip.cornerRadius = 2;
    Chart.defaults.plugins.tooltip.bodyAlign = "center";
    Chart.defaults.plugins.tooltip.titleAlign = "center";

    Chart.defaults.font.family = "graphiklight";
    Chart.defaults.font.size = 14;
    Chart.defaults.font.lineHeight = 1.2;

    Chart.defaults.plugins.legend.display = false;

    Chart.defaults.elements.bar.borderRadius = 1.5;
}

export {
    DestroyOldChart, getGradient,
    chartBase,
};