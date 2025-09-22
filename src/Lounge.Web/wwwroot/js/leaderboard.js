var season = document.getElementById("seasonInput").value;
var game = document.getElementById("gameInput").value;
var isCurrentSeason = document.getElementById("isCurrentSeasonInput").value === "True";
var extra = (document.getElementById("extraInput")?.value || "0") === "1";

function refreshLeaderboard(resetPage = true) {
  const pageSize = 50;
  var pageNumberInputElement = document.getElementById("pageNumberInput");
  var page = resetPage ? 1 : parseInt(pageNumberInputElement.value);

  console.log("Refreshing leaderboard");
  var currentNameFilter = document.getElementById("nameFilter").value;
  var currentCountryFilter = document.getElementById("countryFilter").value;
  var currentMinMmrFilter = document.getElementById("minMmrFilter").value;
  var currentMaxMmrFilter = document.getElementById("maxMmrFilter").value;
  var currentMinEventsFilter = document.getElementById("minEventsFilter").value;
  var currentMaxEventsFilter = document.getElementById("maxEventsFilter").value;
  var currentSortBy = document.getElementById("sortBySelect").value;

  var minCreationDate = document.getElementById("minCreationDate");
  var maxCreationDate = document.getElementById("maxCreationDate");

  var queryParams = [];
  queryParams.push(`game=${game}`);
  queryParams.push(`season=${season}`);
  if (page > 1) queryParams.push(`skip=${(page - 1) * pageSize}`);
  if (pageSize !== 50) queryParams.push(`pageSize=${pageSize}`);
  if (currentSortBy !== "Mmr") queryParams.push(`sortBy=${currentSortBy}`);
  if (currentNameFilter) queryParams.push(`search=${encodeURIComponent(currentNameFilter)}`);
  if (currentCountryFilter) queryParams.push(`country=${currentCountryFilter}`);
  if (currentMinMmrFilter) queryParams.push(`minMmr=${currentMinMmrFilter}`);
  if (currentMaxMmrFilter) queryParams.push(`maxMmr=${currentMaxMmrFilter}`);
  if (currentMinEventsFilter) queryParams.push(`minEventsPlayed=${currentMinEventsFilter}`);
  if (currentMaxEventsFilter) queryParams.push(`maxEventsPlayed=${currentMaxEventsFilter}`);

  if (extra) {
    if (minCreationDate?.value) {
      const dt = new Date(minCreationDate.value);
      queryParams.push(`minCreationDateUtc=${encodeURIComponent(dt.toISOString())}`);
    }
    if (maxCreationDate?.value) {
      const dt = new Date(maxCreationDate.value);
      queryParams.push(`maxCreationDateUtc=${encodeURIComponent(dt.toISOString())}`);
    }
  }

  var query = queryParams.join('&');
  var url = `/api/player/leaderboard?${query}`;
  fetch(url)
    .then(response => response.json())
    .then(data => updateLeaderboard(data, page, pageSize))
    .catch((error) => {
      const colspan = computeColspan();
      document.getElementById("leaderboardTableBody").innerHTML = `<tr><td colspan="${colspan}">Error loading leaderboard data: ${error}</td></tr>`
    });
}

function computeColspan() {
  let cols = 7; // base columns (Rank, country, name, mmr, peak mmr, [last week?], events)
  if (!isCurrentSeason) cols -= 1; // remove last week
  if (extra) {
    // account created + avg score(s)
    cols += 1; // account created
    if (game === 'mk8dx') cols += 1; else cols += 2;
  }
  return cols;
}

function updateLeaderboard(leaderboardData, page, pageSize) {
  if (leaderboardData.data.length == 0) {
    const colspan = computeColspan();
    document.getElementById("leaderboardTableBody").innerHTML = `<tr><td colspan="${colspan}">No players found matching the filter</td></tr>`;
    return;
  }

  var newBody = document.createElement("tbody");
  for (var player of leaderboardData.data) {
    var tr = document.createElement("tr");

    function appendCell(content, ...withClass) {
      var newCell = document.createElement("td");
      if (withClass)
        newCell.classList.add(...withClass);
      newCell.appendChild(content);
      tr.appendChild(newCell);
      return newCell;
    }

    // Rank Column
    var rankCell = document.createElement("th");
    rankCell.scope = "row";
    rankCell.innerHTML = player.overallRank || "-";
    rankCell.classList.add("rank-col")
    tr.appendChild(rankCell);

    // Country Column
    if (player.countryCode) {
      var countryImgElement = document.createElement("img");
      countryImgElement.src = `/img/flags/${player.countryCode}.png`;
      countryImgElement.loading = "lazy";
      appendCell(countryImgElement, "country-col");
    } else {
      appendCell(document.createDocumentFragment());
    }

    // Name Column
    var playerLinkElement = document.createElement("a");
    playerLinkElement.href = `/${game}/PlayerDetails/${player.id}?season=${season}`;
    playerLinkElement.appendChild(document.createTextNode(player.name));
    appendCell(playerLinkElement, "name-col");

    // MMR column with placement handling
    var mmrValue = player.mmr >= 0 ? player.mmr : "Placement";
    var mmrCell = appendCell(document.createTextNode(mmrValue), "rank-color", "equal-width-col");
    if (player.mmrRank && player.mmrRank.division) mmrCell.dataset.rank = player.mmrRank.division;

    var maxMmrValue = player.maxMmr >= 0 ? player.maxMmr : "—";
    var maxMmrCell = appendCell(document.createTextNode(maxMmrValue), "rank-color", "equal-width-col");
    if (player.maxMmrRank && player.maxMmrRank.division) maxMmrCell.dataset.rank = player.maxMmrRank.division;

    // Last Week column with arrows and colors (rank change) - only for current season
    if (isCurrentSeason) {
      var lastWeekCell = document.createElement("td");
      lastWeekCell.classList.add("equal-width-col");
      if (player.lastWeekRankChange === null || player.lastWeekRankChange === undefined) {
        lastWeekCell.innerHTML = "—";
        lastWeekCell.classList.add("rank-change-none");
      } else if (player.lastWeekRankChange < 0) {
        lastWeekCell.innerHTML = `${Math.abs(player.lastWeekRankChange)}&nbsp;⮝`;
        lastWeekCell.classList.add("rank-change-up");
      } else if (player.lastWeekRankChange > 0) {
        lastWeekCell.innerHTML = `${player.lastWeekRankChange}&nbsp;⮟`;
        lastWeekCell.classList.add("rank-change-down");
      } else {
        lastWeekCell.innerHTML = "=";
        lastWeekCell.classList.add("rank-change-same");
      }
      tr.appendChild(lastWeekCell);
    }

    // Extra columns
    if (extra) {
      // Account creation date
      var dateCell = document.createElement("td");
      dateCell.classList.add("equal-width-col");
      if (player.accountCreationDateUtc) {
        // Render in user's locale
        var dt = new Date(player.accountCreationDateUtc);
        dateCell.textContent = dt.toLocaleDateString();
      } else {
        dateCell.textContent = "—";
      }
      tr.appendChild(dateCell);

      // Average score columns
      if (game === 'mk8dx') {
        var avg = player.averageScore12P ?? null;
        appendCell(document.createTextNode(avg != null ? avg.toFixed(1) : "-"), "equal-width-col");
      } else {
        var avg12 = player.averageScore12P ?? null;
        var avg24 = player.averageScore24P ?? null;
        appendCell(document.createTextNode(avg12 != null ? avg12.toFixed(1) : "-"), "equal-width-col");
        appendCell(document.createTextNode(avg24 != null ? avg24.toFixed(1) : "-"), "equal-width-col");
      }
    }

    appendCell(document.createTextNode(player.eventsPlayed), "events-col", "equal-width-col");

    newBody.appendChild(tr);
  }

  var leaderboardBodyElement = document.getElementById("leaderboardTableBody");
  leaderboardBodyElement.id = "";
  leaderboardBodyElement.parentElement.replaceChild(newBody, leaderboardBodyElement);
  newBody.id = "leaderboardTableBody"

  var prevPageButtonElement = document.getElementById("prevPageButton");
  var nextPageButtonElement = document.getElementById("nextPageButton");

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

  var pageNumberInputElement = document.getElementById("pageNumberInput");
  pageNumberInputElement.value = page;
  pageNumberInputElement.max = numPages;

  document.getElementById("maxPageNumber").innerHTML = numPages;
}

function onPageChanged() {
  var pageNumberInputElement = document.getElementById("pageNumberInput");
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
  var pageNumberInputElement = document.getElementById("pageNumberInput");
  pageNumberInputElement.value = parseInt(pageNumberInputElement.value) - 1;
  refreshLeaderboard(false);
}

function nextPage() {
  var pageNumberInputElement = document.getElementById("pageNumberInput");
  pageNumberInputElement.value = parseInt(pageNumberInputElement.value) + 1;
  refreshLeaderboard(false);
}

let debounceLeaderboardTimer;
function debouncedLeaderboard() {
  clearTimeout(debounceLeaderboardTimer);
  debounceLeaderboardTimer = setTimeout(() => refreshLeaderboard(), 500);
}

let debouncePageChangeTimer;
function debouncedPageChange() {
  clearTimeout(debouncePageChangeTimer);
  debouncePageChangeTimer = setTimeout(() => onPageChanged(), 500);
}

document.addEventListener("DOMContentLoaded", function () {

  let leaderboardFormInputs = document.getElementById("leaderboardFormInputs").getElementsByClassName("form-control");
  for (let formInput of leaderboardFormInputs) {
    formInput.addEventListener("input", debouncedLeaderboard);
  }

  document.getElementById("pageNumberInput").addEventListener("input", debouncedPageChange);
  document.getElementById("prevPageButton").addEventListener("click", function (e) { e.preventDefault(); prevPage(); });
  document.getElementById("nextPageButton").addEventListener("click", function (e) { e.preventDefault(); nextPage(); });

  refreshLeaderboard();
});