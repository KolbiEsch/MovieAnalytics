import { DestroyOldChart } from "./chartHelpers.js";

function CreatePieChart(labelsArr, dataArr, pieSum, sliceColorArr, chartID) {
    DestroyOldChart(chartID);

    let canvasID = document.getElementById(chartID);
    const PieChart = new Chart(canvasID, {
        type: "pie",
        data: {
            labels: labelsArr,
            datasets: [{
                data: dataArr,
                backgroundColor: sliceColorArr,
                borderColor: "transparent"
            }]
        },
        options: {
            plugins: {
                tooltip: createPieToolip(pieSum)
            },
            maintainAspectRatio: false
        }
    });
}

function createPieToolip(pieSum) {
    const pieChartTooltip = {
        callbacks: {
            title: function (context) {
                let percentOfTotalMovies = (context[0].parsed / pieSum) * 100;
                return percentOfTotalMovies.toFixed(1) + "%";
            },
            beforeLabel: function (context) {
                return `(${context.parsed} of ${pieSum})`;
            },
            label: function (context) {
                return "";
            }
        },
        titleFont: {
            weight: "normal"
        },
        titleMarginBottom: 3,
        bodyFont: {
            family: "graphikmedium",
            size: 12
        },
        bodyColor: "#99AABB"
    }

    return pieChartTooltip;
}

export { CreatePieChart };