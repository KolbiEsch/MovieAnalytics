import { DestroyOldChart } from "./chartHelpers.js";

const barChartTooltip = {
    callbacks: {
        title: function (context) {
            return "";
        },
        label: function (context) {
            return context.parsed.x + " films";
        }
    },
    backgroundColor: "transparent",
    caretPadding: 0,
    caretSize: 0,
    position: "barTooltipPositioner"
};

const barChartScales = {
    xAxis: {
        display: false
    },
    yAxis: {
        ticks: {
            color: '#ffffff'
        },
        grid: {
            display: false
        }
    }
};

Chart.Tooltip.positioners.barTooltipPositioner = function (elements, eventPosition) {

    if (elements.length == 0) {
        return false;
    }

    return {
        x: elements[0].element.x - elements[0].element.width,
        y: elements[0].element.y
    };
};

function CreateBarChart(labelsArr, dataArr, barColor, chartID) {
    DestroyOldChart(chartID);

    let chartCanvas = document.getElementById(chartID);

    setHeightOfChart(labelsArr.length, chartCanvas);

    const BarChart = new Chart(chartCanvas, {
        type: 'bar',
        data: {
            labels: labelsArr,
            datasets: [{
                label: "Total",
                data: dataArr,
                backgroundColor: barColor
            }]
        },
        options: {
            plugins: {
                tooltip: barChartTooltip
            },
            scales: barChartScales,
            indexAxis: 'y',
            maintainAspectRatio: false
        }
    });
}

function setHeightOfChart(numOfItems, chartCanvas) {
    let chartContainer = chartCanvas.parentElement;
    chartContainer.style.height = `${40 * numOfItems}px`;
}

export { CreateBarChart };