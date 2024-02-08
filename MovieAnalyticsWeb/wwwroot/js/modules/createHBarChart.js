import { DestroyOldChart } from "./chartHelpers.js";

const hBarTooltip = {
    callbacks: {
        title: function (context) {
            return "";
        },
        label: function (context) {
            return context.parsed.y + " films";
        }
    }
};

const hBarScales = {
    xAxis: {
        ticks: {
            color: "#ffffff",
            padding: -25,
            z: 3,
            font: {
                family: "graphikextralight",
                weight: "bold"
            },
            callback: function (value, index, ticks) {
                let label = this.getLabelForValue(value);
                return label[0];
            }
        },
        grid: {
            display: false
        }
    },
    yAxis: {
        display: false
    }
};

function CreateHBarChart(labelsArr, dataArr, chartID, barColor) {
    DestroyOldChart(chartID);

    let canvasID = document.getElementById(chartID);
    const BarChart = new Chart(canvasID, {
        type: 'bar',
        data: {
            labels: labelsArr,
            datasets: [{
                data: dataArr,
                backgroundColor: barColor,
                hoverBackgroundColor: "#00CE26",
                barThickness: "flex",
                barPercentage: 0.93,
                categoryPercentage: 1,
                maxBarThickness: 40
            }]
        },
        options: {
            plugins: {
                tooltip: hBarTooltip
            },
            scales: hBarScales,
            indexAxis: 'x',
            maintainAspectRatio: false
        }
    });
}

export { CreateHBarChart };