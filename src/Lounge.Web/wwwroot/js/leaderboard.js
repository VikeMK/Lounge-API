var season = document.getElementById("seasonInput").value;
var nameFilterElement = document.getElementById("nameFilter");
var countryFilterElement = document.getElementById("countryFilter");
var minMmrFilterElement = document.getElementById("minMmrFilter");
var maxMmrFilterElement = document.getElementById("maxMmrFilter");
var minEventsFilterElement = document.getElementById("minEventsFilter");
var maxEventsFilterElement = document.getElementById("maxEventsFilter");
var sortBySelectElement = document.getElementById("sortBySelect");

var pageNumberInputElement = document.getElementById("pageNumberInput");
var maxPageNumberElement = document.getElementById("maxPageNumber");
var prevPageButtonElement = document.getElementById("prevPageButton");
var nextPageButtonElement = document.getElementById("nextPageButton");

var pageSize = 50;

function refreshLeaderboard(resetPage = true) {
    var page = resetPage ? 1 : parseInt(pageNumberInputElement.value);

    console.log("Refreshing leaderboard");
    var currentNameFilter = nameFilterElement.value;
    var currentCountryFilter = countryFilterElement.value;
    var currentMinMmrFilter = minMmrFilterElement.value;
    var currentMaxMmrFilter = maxMmrFilterElement.value;
    var currentMinEventsFilter = minEventsFilterElement.value;
    var currentMaxEventsFilter = maxEventsFilterElement.value;
    var currentSortBy = sortBySelectElement.value;

    var queryParams = [];
    queryParams.push(`season=${season}`);
    if (page > 1) queryParams.push(`skip=${(page - 1) * pageSize}`);
    if (pageSize !== 50) queryParams.push(`pageSize=${pageSize}`);
    if (currentSortBy !== "Mmr") queryParams.push(`sortBy=${currentSortBy}`);
    if (currentNameFilter) queryParams.push(`search=${currentNameFilter}`);
    if (currentCountryFilter) queryParams.push(`country=${currentCountryFilter}`);
    if (currentMinMmrFilter) queryParams.push(`minMmr=${currentMinMmrFilter}`);
    if (currentMaxMmrFilter) queryParams.push(`maxMmr=${currentMaxMmrFilter}`);
    if (currentMinEventsFilter) queryParams.push(`minEventsPlayed=${currentMinEventsFilter}`);
    if (currentMaxEventsFilter) queryParams.push(`maxEventsPlayed=${currentMaxEventsFilter}`);

    var query = queryParams.join('&');
    var url = `/api/player/leaderboard?${query}`;
    fetch(url)
        .then(response => response.json())
        .then(data => updateLeaderboard(data, page, pageSize))
        .catch((error) => {
            document.getElementById("leaderboardTableBody").innerHTML = `<tr><td colspan="11">Error loading leaderboard data: ${error}</td></tr>`
        });
}

function updateLeaderboard(leaderboardData, page, pageSize) {
    if (leaderboardData.data.length == 0) {
        document.getElementById("leaderboardTableBody").innerHTML = `<tr><td colspan="11">No players found matching the filter</td></tr>`;
        document.getElementById("leaderboard").classList.remove("d-none");
        return;
    }

    var newBody = document.createElement("tbody");
    for (var player of leaderboardData.data) {
        var tr = document.createElement("tr");

        function appendCell(content, withClass) {
            var newCell = document.createElement("td");
            if (withClass)
                newCell.classList.add(withClass);
            newCell.appendChild(content);
            tr.appendChild(newCell);
        }

        var mmrRankClass = player.mmrRank ? `rank-${player.mmrRank.division}` : null;

        // Rank Column
        var rankCell = document.createElement("th");
        rankCell.scope = "row";
        if (mmrRankClass)
            rankCell.classList.add(mmrRankClass);
        rankCell.innerHTML = player.overallRank || "-";
        tr.appendChild(rankCell);

        // Country Column
        if (player.countryCode) {
            var countryImgElement = document.createElement("img");
            countryImgElement.src = `/img/flags/${player.countryCode}.png`;
            countryImgElement.style = "width: 30px"
            appendCell(countryImgElement);
        } else {
            appendCell(document.createDocumentFragment());
        }

        // Name Column
        var playerLinkElement = document.createElement("a");
        playerLinkElement.href = `/PlayerDetails/${player.id}?season=${season}`;
        playerLinkElement.style = "color:inherit";
        playerLinkElement.appendChild(document.createTextNode(player.name));
        appendCell(playerLinkElement, mmrRankClass);

        appendCell(document.createTextNode(player.mmr >= 0 ? player.mmr : "Placement"), mmrRankClass);
        appendCell(document.createTextNode(player.maxMmr >= 0 ? player.maxMmr : "N/A"), player.maxMmrRank ? `rank-${player.maxMmrRank.division}` : null);

        appendCell(document.createTextNode(player.eventsPlayed));
        appendCell(document.createTextNode(player.winRate >= 0 ? `${(player.winRate * 100).toFixed(1)}%` : "N/A"));
        appendCell(document.createTextNode(player.noSQAverageScore >= 0 ? player.noSQAverageScore.toFixed(1) : "N/A"));
        appendCell(document.createTextNode(`${player.winsLastTen} - ${player.lossesLastTen}`));
        appendCell(document.createTextNode(player.gainLossLastTen === undefined ? "N/A" : `${player.gainLossLastTen > 0 ? "+" : ""}${player.gainLossLastTen}`));
        appendCell(document.createTextNode(player.noSQAverageScoreLastTen >= 0 ? player.noSQAverageScoreLastTen.toFixed(1) : "N/A"));

        newBody.appendChild(tr);
    }

    var leaderboardBodyElement = document.getElementById("leaderboardTableBody");
    leaderboardBodyElement.id = "";
    leaderboardBodyElement.parentElement.replaceChild(newBody, leaderboardBodyElement);
    newBody.id = "leaderboardTableBody"

    if (page == 1) {
        prevPageButtonElement.classList.add('disabled');
    } else {
        prevPageButtonElement.classList.remove('disabled');
    }

    var totalPlayers = leaderboardData.totalPlayers;
    var numPages = Math.ceil(totalPlayers / pageSize);
    if (page == numPages) {
        nextPageButtonElement.classList.add('disabled');
    } else {
        nextPageButtonElement.classList.remove('disabled');
    }

    pageNumberInputElement.value = page;
    pageNumberInputElement.max = numPages;
    maxPageNumberElement.innerHTML = numPages;

    document.getElementById("leaderboard").classList.remove("d-none");
}

function onPageChanged() {
    var page = parseInt(pageNumberInputElement.value);
    var maxPage = parseInt(pageNumberInputElement.max);
    if (page < 1) {
        pageNumberInputElement.value = 1;
    } else if (page > maxPage) {
        pageNumberInputElement.value = maxPage;
    }

    refreshLeaderboard(false);
}

function prevPage() {
    pageNumberInputElement.value = parseInt(pageNumberInputElement.value) - 1;
    refreshLeaderboard(false);
}

function nextPage() {
    pageNumberInputElement.value = parseInt(pageNumberInputElement.value) + 1;
    refreshLeaderboard(false);
}

var nameFilter = undefined;
function refreshOnNewNameFilter() {
    var currentNameFilter = nameFilterElement.value;
    if (currentNameFilter !== nameFilter) {
        if (nameFilter !== undefined)
            refreshLeaderboard();
        nameFilter = currentNameFilter;
    }
    setTimeout(refreshOnNewNameFilter, 1000);
}

document.addEventListener("DOMContentLoaded", function () {
    refreshLeaderboard();
    refreshOnNewNameFilter();
});