const chartSeasonConfig = {
  Seasons: {
    4: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 2000,
        Bronze: 4000,
        Silver: 5500,
        Gold: 7000,
        Platinum: 8500,
        Sapphire: 10000,
        Diamond: 11500,
        Master: 13000,
        Grandmaster: 14500,
      },
      RecordsTierOrder: ["X", "S", "A", "AB", "B", "C", "D", "E", "F"],
      DivisionsToTier: {
        X: ["Diamond", "Master"],
        S: ["Sapphire", "Diamond", "Master"],
        A: ["Platinum", "Sapphire", "Diamond", "Master"],
        AB: ["Gold", "Platinum"],
        B: ["Gold"],
        C: ["Silver"],
        D: ["Bronze"],
        E: ["Iron 2", "Bronze", "Placement"],
        F: ["Iron 1", "Iron 2"],
      },
    },
    5: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 1000,
        "Bronze 1": 2000,
        "Bronze 2": 3000,
        "Silver 1": 4000,
        "Silver 2": 5000,
        "Gold 1": 6000,
        "Gold 2": 7000,
        "Platinum 1": 8000,
        "Platinum 2": 9000,
        Sapphire: 10000,
        "Diamond 1": 11000,
        "Diamond 2": 12000,
        Master: 13000,
        Grandmaster: 14000,
      },
      RecordsTierOrder: [
        "X",
        "S",
        "A",
        "B",
        "BC",
        "C",
        "CD",
        "D",
        "DE",
        "E",
        "EF",
        "F",
      ],
      DivisionsToTier: {
        X: ["Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        S: ["Sapphire", "Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        A: [
          "Platinum 2",
          "Sapphire",
          "Diamond 1",
          "Diamond 2",
          "Master",
          "Grandmaster",
        ],
        B: ["Platinum 1", "Platinum 2"],
        BC: ["Gold 2", "Platinum 1"],
        C: ["Gold 1", "Gold 2"],
        CD: ["Silver 2", "Gold 1"],
        D: ["Silver 1", "Silver 2"],
        DE: ["Bronze 2", "Silver 1"],
        E: ["Bronze 1", "Bronze 2"],
        EF: ["Iron 2", "Bronze 1"],
        F: ["Iron 1", "Iron 2", "Placement"],
      },
    },
    6: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 1000,
        "Bronze 1": 2000,
        "Bronze 2": 3000,
        "Silver 1": 4000,
        "Silver 2": 5000,
        "Gold 1": 6000,
        "Gold 2": 7000,
        "Platinum 1": 8000,
        "Platinum 2": 9000,
        "Sapphire 1": 10000,
        "Sapphire 2": 11000,
        "Diamond 1": 12000,
        "Diamond 2": 13000,
        Master: 14000,
        Grandmaster: 15000,
      },
      RecordsTierOrder: [
        "X",
        "S",
        "A",
        "AB",
        "B",
        "BC",
        "C",
        "CD",
        "D",
        "DE",
        "E",
        "EF",
        "F",
      ],
      DivisionsToTier: {
        X: ["Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        S: ["Sapphire 2", "Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        A: [
          "Sapphire 1",
          "Sapphire 2",
          "Diamond 1",
          "Diamond 2",
          "Master",
          "Grandmaster",
        ],
        AB: ["Platinum 2", "Sapphire 1"],
        B: ["Platinum 1", "Platinum 2"],
        BC: ["Gold 2", "Platinum 1"],
        C: ["Gold 1", "Gold 2"],
        CD: ["Silver 2", "Gold 1"],
        D: ["Silver 1", "Silver 2"],
        DE: ["Bronze 2", "Silver 1"],
        E: ["Bronze 1", "Bronze 2"],
        EF: ["Iron 2", "Bronze 1"],
        F: ["Iron 1", "Iron 2", "Placement"],
      },
    },
    7: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 1000,
        "Bronze 1": 2000,
        "Bronze 2": 3000,
        "Silver 1": 4000,
        "Silver 2": 5000,
        "Gold 1": 6000,
        "Gold 2": 7000,
        "Platinum 1": 8000,
        "Platinum 2": 9000,
        "Sapphire 1": 10000,
        "Sapphire 2": 11000,
        "Diamond 1": 12000,
        "Diamond 2": 13000,
        Master: 14000,
        Grandmaster: 15000,
      },
      RecordsTierOrder: [
        "X",
        "S",
        "A",
        "AB",
        "B",
        "BC",
        "C",
        "CD",
        "D",
        "DE",
        "E",
        "EF",
        "F",
      ],
      DivisionsToTier: {
        X: ["Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        S: ["Sapphire 2", "Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        A: [
          "Sapphire 1",
          "Sapphire 2",
          "Diamond 1",
          "Diamond 2",
          "Master",
          "Grandmaster",
        ],
        AB: ["Platinum 2", "Sapphire 1"],
        B: ["Platinum 1", "Platinum 2"],
        BC: ["Gold 2", "Platinum 1"],
        C: ["Gold 1", "Gold 2"],
        CD: ["Silver 2", "Gold 1"],
        D: ["Silver 1", "Silver 2"],
        DE: ["Bronze 2", "Silver 1"],
        E: ["Bronze 1", "Bronze 2"],
        EF: ["Iron 2", "Bronze 1"],
        F: ["Iron 1", "Iron 2", "Placement"],
      },
    },
    8: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 1000,
        "Bronze 1": 2000,
        "Bronze 2": 3000,
        "Silver 1": 4000,
        "Silver 2": 5000,
        "Gold 1": 6000,
        "Gold 2": 7000,
        "Platinum 1": 8000,
        "Platinum 2": 9000,
        "Sapphire 1": 10000,
        "Sapphire 2": 11000,
        "Ruby 1": 12000,
        "Ruby 2": 13000,
        "Diamond 1": 14000,
        "Diamond 2": 15000,
        Master: 16000,
        Grandmaster: 17000,
      },
      RecordsTierOrder: [
        "X",
        "S",
        "A",
        "AB",
        "B",
        "BC",
        "C",
        "CD",
        "D",
        "DE",
        "E",
        "EF",
        "F",
        "FG",
        "G",
      ],
      DivisionsToTier: {
        X: ["Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        S: ["Ruby 2", "Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        A: [
          "Ruby 1",
          "Ruby 2",
          "Diamond 1",
          "Diamond 2",
          "Master",
          "Grandmaster",
        ],
        AB: ["Sapphire 2", "Ruby 1"],
        B: ["Sapphire 1", "Sapphire 2"],
        BC: ["Platinum 2", "Sapphire 1"],
        C: ["Platinum 1", "Platinum 2"],
        CD: ["Gold 2", "Platinum 1"],
        D: ["Gold 1", "Gold 2"],
        DE: ["Silver 2", "Gold 1"],
        E: ["Silver 1", "Silver 2"],
        EF: ["Bronze 2", "Silver 1"],
        F: ["Bronze 1", "Bronze 2"],
        FG: ["Iron 2", "Bronze 1"],
        G: ["Iron 1", "Iron 2", "Placement"],
      },
    },
    9: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 1000,
        "Bronze 1": 2000,
        "Bronze 2": 3000,
        "Silver 1": 4000,
        "Silver 2": 5000,
        "Gold 1": 6000,
        "Gold 2": 7000,
        "Platinum 1": 8000,
        "Platinum 2": 9000,
        "Sapphire 1": 10000,
        "Sapphire 2": 11000,
        "Ruby 1": 12000,
        "Ruby 2": 13000,
        "Diamond 1": 14000,
        "Diamond 2": 15000,
        Master: 16000,
        Grandmaster: 17000,
      },
      RecordsTierOrder: [
        "X",
        "S",
        "A",
        "AB",
        "B",
        "BC",
        "C",
        "CD",
        "D",
        "DE",
        "E",
        "EF",
        "F",
        "FG",
        "G",
      ],
      DivisionsToTier: {
        X: ["Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        S: ["Ruby 2", "Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        A: [
          "Ruby 1",
          "Ruby 2",
          "Diamond 1",
          "Diamond 2",
          "Master",
          "Grandmaster",
        ],
        AB: ["Sapphire 2", "Ruby 1"],
        B: ["Sapphire 1", "Sapphire 2"],
        BC: ["Platinum 2", "Sapphire 1"],
        C: ["Platinum 1", "Platinum 2"],
        CD: ["Gold 2", "Platinum 1"],
        D: ["Gold 1", "Gold 2"],
        DE: ["Silver 2", "Gold 1"],
        E: ["Silver 1", "Silver 2"],
        EF: ["Bronze 2", "Silver 1"],
        F: ["Bronze 1", "Bronze 2"],
        FG: ["Iron 2", "Bronze 1"],
        G: ["Iron 1", "Iron 2", "Placement"],
      },
    },
    10: {
      Ranks: {
        "Iron 1": 0,
        "Iron 2": 1000,
        "Bronze 1": 2000,
        "Bronze 2": 3000,
        "Silver 1": 4000,
        "Silver 2": 5000,
        "Gold 1": 6000,
        "Gold 2": 7000,
        "Platinum 1": 8000,
        "Platinum 2": 9000,
        "Sapphire 1": 10000,
        "Sapphire 2": 11000,
        "Ruby 1": 12000,
        "Ruby 2": 13000,
        "Diamond 1": 14000,
        "Diamond 2": 15000,
        Master: 16000,
        Grandmaster: 17000,
      },
      RecordsTierOrder: [
        "X",
        "S",
        "A",
        "AB",
        "B",
        "BC",
        "C",
        "CD",
        "D",
        "DE",
        "E",
        "EF",
        "F",
        "FG",
        "G",
      ],
      DivisionsToTier: {
        X: ["Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        S: ["Ruby 2", "Diamond 1", "Diamond 2", "Master", "Grandmaster"],
        A: [
          "Ruby 1",
          "Ruby 2",
          "Diamond 1",
          "Diamond 2",
          "Master",
          "Grandmaster",
        ],
        AB: ["Sapphire 2", "Ruby 1"],
        B: ["Sapphire 1", "Sapphire 2"],
        BC: ["Platinum 2", "Sapphire 1"],
        C: ["Platinum 1", "Platinum 2"],
        CD: ["Gold 2", "Platinum 1"],
        D: ["Gold 1", "Gold 2"],
        DE: ["Silver 2", "Gold 1"],
        E: ["Silver 1", "Silver 2"],
        EF: ["Bronze 2", "Silver 1"],
        F: ["Bronze 1", "Bronze 2"],
        FG: ["Iron 2", "Bronze 1"],
        G: ["Iron 1", "Iron 2", "Placement"],
      },
    },
  },
  Colors: {
    Grandmaster: "#a3022c",
    Master: "#9370db",
    Diamond: "#b9f2ff",
    Ruby: "#d51c5e",
    Sapphire: "#286cd3",
    Platinum: "#3fabb8",
    Gold: "#f1c232",
    Silver: "#cccccc",
    Bronze: "#b45f06",
    Iron: "#817876",
  },
  CountryNames: {
    AF: "Afghanistan",
    AL: "Albania",
    DZ: "Algeria",
    AS: "American Samoa",
    AD: "Andorra",
    AQ: "Antarctica",
    AR: "Argentina",
    AM: "Armenia",
    AW: "Aruba",
    AU: "Australia",
    AT: "Austria",
    AZ: "Azerbaijan",
    BS: "Bahamas",
    BB: "Barbados",
    BY: "Belarus",
    BE: "Belgium",
    BH: "Bhutan",
    BO: "Bolivia",
    BQ: "Bonaire, Sint Eustatius and Saba",
    BA: "Bosnia and Herzegovina",
    BR: "Brazil",
    IO: "British Indian Ocean Territory",
    CA: "Canada",
    CL: "Chile",
    CN: "China",
    CO: "Colombia",
    CG: "Congo",
    CR: "Costa Rica",
    HR: "Croatia",
    CU: "Cuba",
    CY: "Cyprus",
    CZ: "Czech Republic",
    DK: "Denmark",
    DO: "Dominican Republic",
    EC: "Ecuador",
    EG: "Egypt",
    SV: "El Salvador",
    FI: "Finland",
    FR: "France",
    GF: "French Guiana",
    PF: "French Polynesia",
    GE: "Georgia",
    DE: "Germany",
    GR: "Greece",
    GU: "Guam",
    GT: "Guatemala",
    HN: "Honduras",
    HK: "Hong Kong",
    HU: "Hungary",
    IS: "Iceland",
    IN: "India",
    ID: "Indonesia",
    IE: "Ireland",
    IL: "Israel",
    IT: "Italy",
    JM: "Jamaica",
    JP: "Japan",
    JE: "Jersey",
    JO: "Jordan",
    LV: "Latvia",
    LB: "Lebanon",
    LU: "Luxembourg",
    MO: "Macao",
    MG: "Madagascar",
    MY: "Malaysia",
    MT: "Malta",
    MX: "Mexico",
    MA: "Morocco",
    NL: "Netherlands",
    NC: "New Caledonia",
    NZ: "New Zealand",
    NI: "Nicaragua",
    NE: "Niger",
    NO: "Norway",
    PA: "Panama",
    PY: "Paraguay",
    PE: "Peru",
    PH: "Philippines",
    PL: "Poland",
    PT: "Portugal",
    PR: "Puerto Rico",
    RO: "Romania",
    RU: "Russia",
    RE: "R\u00e9union",
    SA: "Saudi Arabia",
    SL: "Sierra Leone",
    SG: "Singapore",
    SK: "Slovakia",
    SI: "Slovenia",
    ZA: "South Africa",
    GS: "South Georgia and the South Sandwich Islands",
    KR: "South Korea",
    ES: "Spain",
    SE: "Sweden",
    CH: "Switzerland",
    TW: "Taiwan",
    TH: "Thailand",
    TT: "Trinidad and Tobago",
    TN: "Tunisia",
    TR: "Turkey",
    TZ: "Tanzania",
    VI: "U.S. Virgin Islands",
    AE: "United Arab Emirates",
    GB: "United Kingdom",
    US: "United States",
    XX: "Unknown",
    UY: "Uruguay",
    VE: "Venezuela",
  },
};

const getDivisionName = (mmr, season) => {
  const currArray = Object.entries(chartSeasonConfig.Seasons[season].Ranks);
  if (mmr === 0) return currArray[0][0].split(" ")[0];
  currArray.push(["currPlayer", mmr]);
  currArray.sort((a, b) => a[1] - b[1]);
  const correctTierIndex =
    currArray.findIndex((division) => division[0] === "currPlayer") - 1;
  return currArray[correctTierIndex][0].split(" ")[0];
};

function appendCell(content, withClass) {
  const newCell = document.createElement("td");
  if (withClass) newCell.classList.add(withClass);
  newCell.appendChild(content);
  tr.appendChild(newCell);
}

const {
  CountryNames: countryNames,
  Colors: colors,
  Seasons,
} = chartSeasonConfig;

function updateMogiTables(data, season) {
  const tierSortOrder = Seasons[season].RecordsTierOrder;
  tierSortOrder.unshift("SQ");
  const mogiTierData = Object.entries(data.activityData.tierActivity).sort(
    (a, b) => tierSortOrder.indexOf(b[0]) - tierSortOrder.indexOf(a[0])
  );

  const tierTableBody = document.getElementById("mogi-tier-table");
  for (const index in mogiTierData) {
    const tier = mogiTierData[index];
    const tr = document.createElement("tr");

    tr.style.color =
      tier[0] === "SQ"
        ? "#FFFFFF"
        : colors[Seasons[season].DivisionsToTier[tier[0]][0].split(" ")[0]];

    // Tier Column
    const tierCell = document.createElement("th");
    tierCell.scope = "row";
    tierCell.innerHTML = tier[0];
    tr.appendChild(tierCell);

    // Mogis Column
    const mogisCell = document.createElement("th");
    mogisCell.scope = "row";
    mogisCell.innerHTML = tier[1];
    tr.appendChild(mogisCell);

    // % of Mogis Column
    const mogiPercentCell = document.createElement("th");
    mogiPercentCell.scope = "row";
    mogiPercentCell.innerHTML = `${
      Math.round((tier[1] / data.totalMogis) * 10000) / 100
    }%`;
    tr.appendChild(mogiPercentCell);

    tierTableBody.appendChild(tr);
  }

  let daysInSeason = 0;
  let mogisPerDay = 0;

  if (Object.keys(data.activityData.dailyActivity).length > 0) {
    const mogiActivity = Object.entries(data.activityData.dailyActivity).sort(
      (a, b) => new Date(a[0]) - new Date(b[0])
    );

    const beginning = new Date(mogiActivity[0][0]).getTime();
    const end = new Date(mogiActivity[mogiActivity.length - 1][0]).getTime();
    daysInSeason = Math.round((end - beginning) / (1000 * 60 * 60 * 24)) + 1;
    mogisPerDay = Math.round((100 * data.totalMogis) / daysInSeason) / 100;
  }

  document.getElementById("days-in-season").innerHTML = daysInSeason;
  document.getElementById("average-mogis-per-day").innerHTML = mogisPerDay;

  const weekdayTableColors = [
    "#a3022c",
    "#9370db",
    "#d51c5e",
    "#286cd3",
    "#f1c232",
    "#cccccc",
    "#b45f06",
  ];
  const weekdayTableSortOrder = [
    "Sunday",
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
  ];
  const mogiWeekdayData = Object.entries(
    data.activityData.dayOfWeekActivity
  ).sort(
    (a, b) =>
      weekdayTableSortOrder.indexOf(a[0]) - weekdayTableSortOrder.indexOf(b[0])
  );

  const weekdayTableBody = document.getElementById("mogi-weekday-table");
  for (const index in mogiWeekdayData) {
    const day = mogiWeekdayData[index];
    const tr = document.createElement("tr");
    tr.style.color = weekdayTableColors[index];

    // Day of Week Column
    const dayOfWeekCell = document.createElement("th");
    dayOfWeekCell.scope = "row";
    dayOfWeekCell.innerHTML = day[0];
    tr.appendChild(dayOfWeekCell);

    // Mogis Column
    const mogisCell = document.createElement("th");
    mogisCell.scope = "row";
    mogisCell.innerHTML = day[1];
    tr.appendChild(mogisCell);

    // % of Mogis Column
    const mogiPercentCell = document.createElement("th");
    mogiPercentCell.scope = "row";
    mogiPercentCell.innerHTML = `${
      Math.round((day[1] / data.totalMogis) * 10000) / 100
    }%`;
    tr.appendChild(mogiPercentCell);

    weekdayTableBody.appendChild(tr);
  }
}

function updateMogiActivityChart(data, season) {
  const mogiActivity = Object.entries(data.activityData.dailyActivity).sort(
    (a, b) => new Date(a[0]) - new Date(b[0])
  );

  const seasonDataset = Seasons[season].RecordsTierOrder.reverse();
  seasonDataset.unshift("SQ");

  const title = (tooltipItems) => {
    const index = tooltipItems[0].dataIndex;
    const date = mogiActivity[index];

    return `${date[0]}`;
  };

  const footer = (tooltipItems) => {
    const index = tooltipItems[0].dataIndex;
    const date = mogiActivity[index];

    const mogiPercentage =
      Math.round(
        (10000 * parseInt(tooltipItems[0].formattedValue)) / date[1].Total
      ) / 100;

    return `${mogiPercentage}% of ${date[1].Total}`;
  };

  new Chart(document.getElementById("statMogiActivityChartBody"), {
    type: "bar",
    data: {
      labels: mogiActivity.map((row) => row[0]),
      datasets: seasonDataset.map((tier) => {
        return {
          label: tier,
          data: mogiActivity.map((date) => date[1][tier]),
          backgroundColor:
            tier !== "SQ"
              ? colors[Seasons[season].DivisionsToTier[tier][0].split(" ")[0]]
              : "#FFFFFF",
        };
      }),
    },
    options: {
      plugins: {
        title: {
          display: true,
          font: {
            size: 16,
          },
          padding: 20,
          text: "Mogis Per Day",
        },
        tooltip: {
          callbacks: {
            title,
            footer,
          },
        },
        legend: {
          display: false,
        },
      },
      scales: {
        x: {
          stacked: true,
        },
        y: {
          stacked: true,
        },
      },
    },
  });
}

function updateMogiFormatChart(data) {
  const formatOrder = ["FFA", "2v2", "3v3", "4v4", "6v6"];
  const mogiColors = ["#d51c5e", "#286cd3", "#f1c232", "#cccccc", "#b45f06"];
  const mogiFormatData = Object.entries(data.activityData.formatData).sort(
    (a, b) => formatOrder.indexOf(a[0]) - formatOrder.indexOf(b[0])
  );
  const noSQMogiTotal = mogiFormatData.reduce((a, b) => a + b[1], 0);

  const windowSize = Math.max(
    document.documentElement.clientWidth,
    window.innerWidth || 0
  );

  new Chart(document.getElementById("statMogiFormatChartBody"), {
    type: "pie",
    data: {
      labels: mogiFormatData.map((row) => row[0]),
      datasets: [
        {
          data: mogiFormatData.map((row) => row[1]),
          backgroundColor: mogiFormatData.map(
            (row, index) => mogiColors[index]
          ),
        },
      ],
    },
    options: {
      responsive: true,
      aspectRatio: windowSize <= 767 ? 1.1 : windowSize <= 991 ? 1.4 : 2,
      onResize: (chart, size) => {
        const windowSize = Math.max(
          document.documentElement.clientWidth,
          window.innerWidth || 0
        );

        chart.options.aspectRatio =
          windowSize <= 767 ? 1.1 : windowSize <= 991 ? 1.4 : 2;
      },
      plugins: {
        title: {
          display: true,
          font: {
            size: 16,
          },
          padding: 20,
          text: "Number of Mogis per Format (No SQ)",
        },
        tooltip: {
          enabled: false,
        },
        legend: {
          position: "bottom",
          title: {
            display: true,
            padding: 4,
          },
          labels: {
            font: {
              size: 16,
            },
          },
        },
      },
    },
  });

  const mogiTableBody = document.getElementById("mogi-format-table");
  for (const index in mogiFormatData) {
    const format = mogiFormatData[index];
    const tr = document.createElement("tr");
    tr.style.color = mogiColors[index];

    // Format Column
    const formatCell = document.createElement("th");
    formatCell.scope = "row";
    formatCell.innerHTML = format[0];
    tr.appendChild(formatCell);

    // Mogis Column
    const mogisCell = document.createElement("th");
    mogisCell.scope = "row";
    mogisCell.innerHTML = format[1];
    tr.appendChild(mogisCell);

    // % of Mogis Column
    const mogiPercentCell = document.createElement("th");
    mogiPercentCell.scope = "row";
    mogiPercentCell.innerHTML = `${
      Math.round((format[1] / noSQMogiTotal) * 10000) / 100
    }%`;
    tr.appendChild(mogiPercentCell);

    mogiTableBody.appendChild(tr);
  }
}

function updateTopCountryChart(data, season) {
  const filtered = Object.entries(data.countryData).filter(
    (a) => a[1].playerTotal >= 6
  );
  const sorted = filtered.sort((a, b) => b[1].topSixMmr - a[1].topSixMmr);

  const title = (tooltipItems) => {
    const index = tooltipItems[0].dataIndex;
    const country = sorted[index];

    return `${countryNames[country[0]]} - #${index + 1}`;
  };

  const footer = (tooltipItems) => {
    const index = tooltipItems[0].dataIndex;
    const country = sorted[index];

    return country[1].topSixPlayers.map((x) => `${x.name} - ${x.mmr}`);
  };

  new Chart(document.getElementById("statTopCountryChartBody"), {
    type: "bar",
    data: {
      labels: sorted.map((row) => row[0]),
      datasets: [
        {
          data: sorted.map((row) => Math.round(row[1].topSixMmr * 100) / 100),
          backgroundColor: sorted.map(
            (row) => colors[getDivisionName(row[1].topSixMmr, season)]
          ),
        },
      ],
    },
    options: {
      plugins: {
        title: {
          display: true,
          font: {
            size: 16,
          },
          padding: 20,
          text: "Top Six Players Average MMR (min. 6)",
        },
        tooltip: {
          callbacks: {
            title,
            footer,
          },
        },
        legend: {
          display: false,
        },
      },
      scales: {
        xAxes: {
          ticks: {
            autoSkip: false,
          },
        },
      },
    },
  });
}

function updateOverallCountryChart(data, season) {
  const filtered = Object.entries(data.countryData).filter(
    (a) => a[1].playerTotal >= 10
  );
  const sorted = filtered.sort(
    (a, b) => b[1].totalAverageMmr - a[1].totalAverageMmr
  );

  const title = (tooltipItems) => {
    const index = tooltipItems[0].dataIndex;
    const country = sorted[index];

    return `${countryNames[country[0]]} - #${index + 1}`;
  };

  const footer = (tooltipItems) => {
    const index = tooltipItems[0].dataIndex;
    const country = sorted[index];

    return "Player Total: " + country[1].playerTotal;
  };

  new Chart(document.getElementById("statOverallCountryChartBody"), {
    type: "bar",
    data: {
      labels: sorted.map((row) => row[0]),
      datasets: [
        {
          data: sorted.map(
            (row) => Math.round(row[1].totalAverageMmr * 100) / 100
          ),
          backgroundColor: sorted.map(
            (row) => colors[getDivisionName(row[1].totalAverageMmr, season)]
          ),
        },
      ],
    },
    options: {
      plugins: {
        title: {
          display: true,
          font: {
            size: 16,
          },
          padding: 20,
          text: "Average Country MMR (min. 10)",
        },
        tooltip: {
          callbacks: {
            title: title,
            footer: footer,
          },
        },
        legend: {
          display: false,
        },
      },
    },
  });
}

function updatePopulationCountryChart(data) {
  const populationCountryColors = Object.values(colors);

  const populationCountryData = Object.entries(data.countryData).sort(
    (a, b) => b[1].playerTotal - a[1].playerTotal
  );

  if (populationCountryData.length >= 10) {
    const topCountriesTotal = populationCountryData
      .slice(0, 9)
      .reduce((a, b) => a + b[1].playerTotal, 0);
    populationCountryData.length = 9;
    populationCountryData.push([
      "Other",
      {
        playerTotal: data.totalPlayers - topCountriesTotal,
      },
    ]);
  }

  const windowSize = Math.max(
    document.documentElement.clientWidth,
    window.innerWidth || 0
  );

  new Chart(document.getElementById("statPopulationCountryChartBody"), {
    type: "pie",
    data: {
      labels: populationCountryData.map((row) => row[0]),
      datasets: [
        {
          data: populationCountryData.map((row) => row[1].playerTotal),
          backgroundColor: populationCountryData.map(
            (row, index) => populationCountryColors[index]
          ),
        },
      ],
    },
    options: {
      responsive: true,
      aspectRatio: windowSize <= 767 ? 1 : windowSize <= 991 ? 1.4 : 2,
      onResize: (chart, size) => {
        const windowSize = Math.max(
          document.documentElement.clientWidth,
          window.innerWidth || 0
        );

        chart.options.aspectRatio =
          windowSize <= 767 ? 1 : windowSize <= 991 ? 1.4 : 2;
      },
      plugins: {
        title: {
          display: true,
          font: {
            size: 16,
          },
          padding: 20,
          text: "Country Population",
        },
        tooltip: {
          enabled: false,
        },
        legend: {
          position: "bottom",
          title: {
            display: true,
            padding: 4,
          },
          labels: {
            font: {
              size: 14,
            },
          },
        },
      },
    },
  });

  const countryPopulationTableBody = document.getElementById(
    "country-population-table"
  );
  for (const index in populationCountryData) {
    const country = populationCountryData[index];
    const tr = document.createElement("tr");
    tr.style.color = populationCountryColors[index];

    // Country Column
    const countryCell = document.createElement("th");
    countryCell.scope = "row";
    countryCell.innerHTML =
      country[0] !== "Other" ? countryNames[country[0]] : "Other";
    tr.appendChild(countryCell);

    // Population Column
    const populationCell = document.createElement("th");
    populationCell.scope = "row";
    populationCell.innerHTML = country[1].playerTotal;
    tr.appendChild(populationCell);

    // % of Total Population Column
    const populationPercentCell = document.createElement("th");
    populationPercentCell.scope = "row";
    populationPercentCell.innerHTML = `${
      Math.round((country[1].playerTotal / data.totalPlayers) * 10000) / 100
    }%`;
    tr.appendChild(populationPercentCell);

    countryPopulationTableBody.appendChild(tr);
  }
}

function updateStats(data) {
  const statsTableBody = document.getElementById("stats-table");
  let currPlayers = data.totalPlayers;
  for (const tier of data.divisionData) {
    const tr = document.createElement("tr");
    tr.style.color = colors[tier.tier.split(" ")[0]];

    // Division Column
    const divisionCell = document.createElement("th");
    divisionCell.scope = "row";
    divisionCell.innerHTML = tier.tier;
    tr.appendChild(divisionCell);

    // Players Column
    const playersCell = document.createElement("th");
    playersCell.scope = "row";
    playersCell.innerHTML = tier.count;
    tr.appendChild(playersCell);

    // % of Players Column
    const percentPlayersCell = document.createElement("th");
    percentPlayersCell.scope = "row";
    percentPlayersCell.innerHTML = `${
      Math.round((tier.count / data.totalPlayers) * 10000) / 100
    }%`;
    tr.appendChild(percentPlayersCell);

    // Percentiles Column
    const percentileCell = document.createElement("th");
    percentileCell.scope = "row";
    const right = Math.round((currPlayers / data.totalPlayers) * 10000) / 100;
    currPlayers -= tier.count;
    const left = Math.round((currPlayers / data.totalPlayers) * 10000) / 100;
    percentileCell.innerHTML = `${left}% - ${right}%`;
    tr.appendChild(percentileCell);

    statsTableBody.appendChild(tr);
  }
  document.getElementById("total-players").innerHTML = data.totalPlayers;
  document.getElementById("total-mogis").innerHTML = data.totalMogis;
  document.getElementById("average-mmr").innerHTML = data.averageMmr;
  document.getElementById("median-mmr").innerHTML = data.medianMmr;
}

function updateStatsChart(data) {
  new Chart(document.getElementById("statDivisionChartBody"), {
    type: "bar",
    data: {
      labels: data.divisionData.map((row) => row.tier),
      datasets: [
        {
          label: "Players per Rank",
          data: data.divisionData.map((row) => row.count),
          backgroundColor: data.divisionData.map(
            (row) => colors[row.tier.split(" ")[0]]
          ),
        },
      ],
    },
    options: {
      plugins: {
        title: {
          display: true,
          font: {
            size: 16,
          },
          padding: 20,
          text: "Players per Rank",
        },
        legend: {
          display: false,
        },
      },
    },
  });
}
