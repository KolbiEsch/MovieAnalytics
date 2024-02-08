import { DestroyOldChart, getGradient } from
    "./chartHelpers.js";

const x = window.matchMedia("(min-width: 600px)");
const gradientColors = ["#00CE26", "#00ce86", "#3BACFD"];

let movieYearArr = [];
let movieYearFilmsSeenArr = [];
let weekArr = [];
let weekFilmsSeenArr = [];
x.addListener(shiftWeekChartColumns);
x.addListener(shiftYearChartColumns);

const gradientBarScales = {
    xAxis: {
        display: false
    },
    yAxis: {
        ticks: {
            display: false
        },
        grid: {
            display: false
        }
    }
};

function CreateGradientBarChart(labelsArr, dataArr, colorsArr, indexAxis, chartID) {
    DestroyOldChart(chartID);

    let canvas = document.getElementById(chartID);

    const BarChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: labelsArr,
            datasets: [{
                data: dataArr,
                backgroundColor: function (context) {
                    const chart = context.chart;
                    const { ctx, chartArea } = chart;

                    if (!chartArea) {
                        return null;
                    }

                    return getGradient(ctx, chartArea, indexAxis, colorsArr);
                }
            }]
        },
        options: {
            plugins: {
                tooltip: getGradientTooltip(indexAxis)
            },
            scales: gradientBarScales,
            indexAxis: indexAxis,
            maintainAspectRatio: false
        }
    });
}

function getGradientTooltip(indexAxis) {
    const tooltip = {
        callbacks: {
            title: function (context) {
                let numOfFilms;
                if (indexAxis == "y") {
                    numOfFilms = context[0].parsed.x;
                } else {
                    numOfFilms = context[0].parsed.y;
                }
                if (numOfFilms == 1) return numOfFilms + " film";
                return numOfFilms + " films";
            },
            beforeLabel: function (context) {
                return context.label;
            },
            label: function (context) {
                return "";
            }
        },
        titleFont: {
            weight: "normal"
        },
        titleMarginBottom: 3,
        bodyColor: "#99AABB",
        bodyFont: {
            family: "graphikmedium",
            size: 12
        },
        padding: {
            left: 10, right: 10, top: 6, bottom: 6
        }
    };
    return tooltip;
}

function createYearChartOnAxis(labelsArr, dataArr) {
    movieYearArr = labelsArr;
    movieYearFilmsSeenArr = dataArr;
    shiftYearChartColumns(x);
}

function shiftYearChartColumns(mediaQuery) {
    if (mediaQuery.matches) {
        CreateGradientBarChart(movieYearArr,
            movieYearFilmsSeenArr, gradientColors,
            "x", "movieYearChart");
    }
    else {
        CreateGradientBarChart(movieYearArr,
            movieYearFilmsSeenArr, gradientColors,
            "y", "movieYearChart");
    }
}

function createWeekChartOnAxis(labelsArr, dataArr) {
    weekArr = labelsArr;
    weekFilmsSeenArr = dataArr;
    shiftWeekChartColumns(x);
}

function shiftWeekChartColumns(mediaQuery) {
    if (mediaQuery.matches) {
        CreateGradientBarChart(weekArr,
            weekFilmsSeenArr, gradientColors,
            "x", "movieWeekChart");
    }
    else {
        CreateGradientBarChart(weekArr,
            weekFilmsSeenArr, gradientColors,
            "y", "movieWeekChart");
    }
}

export {
    CreateGradientBarChart, createYearChartOnAxis,
    createWeekChartOnAxis
};