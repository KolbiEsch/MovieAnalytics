import { CreateBarChart } from
    "./modules/createBarChart.js";
import { CreatePieChart } from
    "./modules/createPieChart.js";
import { CreateHBarChart } from
    "./modules/createHBarChart.js";
import {
    createYearChartOnAxis,
    createWeekChartOnAxis,
    CreateGradientBarChart
} from "./modules/createGradientBarChart.js";
import { chartBase } from "./modules/chartHelpers.js";

const pieColorArr = ["#00e054", "#445566"];

chartBase(Chart);
GetBarData("A Life in Film");

function GetBarData(year) {

    let data = {
        year: year
    }

    $.ajax({
        type: "GET",
        url: "/Home/GetDataByYear",
        dataType: "json",
        data: data,
        success: function (Data) {
            SetGeneralStat("watchedMoviesCount", Data.numberOfMovies);
            SetGeneralStat("hoursWatchedStat", Data.hoursWatched);
            SetGeneralStat("numberOfLanguages", Data.languagesHeard);

            createYearBlock(Data);
            createWeekBlock(Data);
            createGenreLanguageBlock(Data);
            createBreakdownBlock(Data);
        }
    });
}

function createYearBlock(Data) {
    //Set markers for year chart.
    const minMovieYear = document.getElementById("minMovieYear");
    const maxMovieYear = document.getElementById("maxMovieYear");
    minMovieYear.innerHTML = Data.minMovieYear;
    maxMovieYear.innerHTML = Data.maxMovieYear;

    //Create Year Bar Chart
    createYearChartOnAxis(Data.yearList, Data.filmsSeenInYearList);
}

function createWeekBlock(Data) {
    const weekFilmsSeenContainer = document.getElementById("filmsByWeek");
    if (Data.yearOfStats == -1) {
        weekFilmsSeenContainer.classList.remove("is-active");
        return;
    }

    //Create Week Chart
    createWeekChartOnAxis(Data.weekList, Data.filmsSeenInWeekList);

    SetGeneralStat("filmsWatched", Data.numberOfMovies);
    SetGeneralStat("averageMoviesMonth", Data.averageMoviesPerMonth);
    SetGeneralStat("averageMoviesWeek", Data.averageMoviesPerWeek);

    //Create Weekday Chart
    CreateHBarChart(Data.weekdayList, Data.filmsSeenInWeekdayList,
        "weekdayFilmsSeenChart", "#445566");

    weekFilmsSeenContainer.classList.add("is-active");
}

function createGenreLanguageBlock(Data) {
    //Create Genre bar chart
    CreateBarChart(Data.genreNameList,
        Data.genreNumList, "#00e054", "genresChart");

    //Create Language bar chart
    CreateBarChart(Data.languageNameList,
        Data.languageCountList, "#FB7C05", "languagesChart");
}

function createBreakdownBlock(Data) {
    //Create Watches/Rewatches Pie Chart
    let rewatchLabelArr = ["Watches", "Rewatches"];
    let pieDataArr = [Data.numberOfMovies - Data.rewatchCount,
    Data.rewatchCount];
    CreatePieChart(rewatchLabelArr, pieDataArr,
        Data.numberOfMovies, pieColorArr, "rewatchesChart");

    const newReleasesSeenBlock = document
        .getElementById("newReleasesSeenBlock");
    if (Data.yearOfStats == -1) {
        newReleasesSeenBlock.classList.remove("is-active");
        return;
    }

    //Create New Releases Pie Chart
    SetGeneralStat("releases", Data.yearOfStats + " Releases");

    let releasesLabelArr = [Data.yearOfStats + " Releases", "Older"];
    let releasesDataArr = [Data.moviesWatchedStatYear,
    Data.numberOfMovies - Data.moviesWatchedStatYear];
    CreatePieChart(releasesLabelArr, releasesDataArr,
        Data.numberOfMovies, pieColorArr, "newReleasesSeenChart");

    newReleasesSeenBlock.classList.add("is-active");
}

function SetGeneralStat(statID, statData) {
    const statItem = document.getElementById(statID);

    //If data is already in stat, remove the current data.
    if (statItem.children.length == 2) {
        statItem.firstChild.remove();
    }

    const dataNode = document.createElement("span");
    dataNode.innerHTML = statData;
    statItem.prepend(dataNode);
}

let headerBtn = document.querySelector(".header-dropdown__btn");
let dropdownList = document.querySelector(".header-dropdown__list");
let dropdownArrow = document.querySelector(".header-dropdown__arrow");

headerBtn.addEventListener("click", () => {
    dropdownList.classList.toggle("is-active");
});

let headerChoices = document.querySelectorAll(".header-dropdown__choice");
for (let i = 0; i < headerChoices.length; i++) {
    headerChoices[i].addEventListener("click", SwapHeaders)
}

function SwapHeaders() {
    let currentHeader = headerBtn.firstChild.nodeValue;
    headerBtn.firstChild.nodeValue = this.innerHTML;
    this.innerHTML = currentHeader;
    sortList(dropdownList);
    GetBarData(headerBtn.firstChild.nodeValue.trim());
}

function sortList(ul) {
    // Add all <li>s to an array
    let lis = [];
    let notNum;
    let containedString = false;
    for (let i = 0; i < ul.childNodes.length; i++) {
        if (ul.childNodes[i].nodeName === "LI") {
            if (ul.childNodes[i].childNodes[0].nodeValue.trim() == "All Time") {
                notNum = ul.childNodes[i];
                containedString = true;
                continue;
            }
            lis.push(ul.childNodes[i]);
        }
    }

    // Sort the lis in descending order
    lis.sort(function (a, b) {
        return parseInt(b.childNodes[0].nodeValue, 10) -
               parseInt(a.childNodes[0].nodeValue, 10);
    });

    if (containedString) {
        lis.unshift(notNum);
    }
    
    ul.replaceChildren(...lis);
}