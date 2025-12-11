document.addEventListener('DOMContentLoaded', () => {
    const folderInput = document.getElementById('folderInput');
    const cardSelector = document.getElementById('cardSelector');

    let allReports = [];
    let charts = {};

    folderInput.addEventListener('change', async (event) => {
        const files = event.target.files;
        if (files.length === 0) return;

        console.log(`Loading ${files.length} report files...`);

        const filePromises = Array.from(files)
            .filter(file => file.name.endsWith('.json'))
            .map(file => {
                return new Promise((resolve, reject) => {
                    const reader = new FileReader();
                    reader.onload = (e) => resolve(JSON.parse(e.target.result));
                    reader.onerror = reject;
                    reader.readAsText(file);
                });
            });

        allReports = await Promise.all(filePromises);
        allReports.sort((a, b) => a.Generation - b.Generation);

        console.log('All reports loaded and sorted.', allReports);
        
        Object.values(charts).forEach(chart => chart.destroy());

        renderFitnessChart();
        renderWinRateHpChart();
        renderCardViabilityChart();
        populateCardSelector();
        renderCardTrendChart();
    });

    cardSelector.addEventListener('change', () => {
        if(charts.cardTrend) charts.cardTrend.destroy();
        renderCardTrendChart();
    });

    function renderFitnessChart() {
        const ctx = document.getElementById('fitnessChart').getContext('2d');
        const labels = allReports.map(r => `Gen ${r.Generation}`);
        const fitnessData = allReports.map(r => r.Fitness);
        
        charts.fitness = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Overall Fitness',
                    data: fitnessData,
                    borderColor: 'rgb(75, 192, 192)',
                    tension: 0.1,
                    fill: false
                }]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Fitness Score Over Generations' } }
            }
        });
    }

    function renderWinRateHpChart() {
        const ctx = document.getElementById('winRateHpChart').getContext('2d');
        const labels = allReports.map(r => `Gen ${r.Generation}`);
        const winRateData = allReports.map(r => r.Report.WinRate * 100);
        const avgHpData = allReports.map(r => r.Report.AvgHpOnVictory * 100);

        charts.winRateHp = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Win Rate (%)',
                    data: winRateData,
                    borderColor: 'rgb(54, 162, 235)',
                    yAxisID: 'y'
                }, {
                    label: 'Avg HP on Victory (%)',
                    data: avgHpData,
                    borderColor: 'rgb(255, 99, 132)',
                    yAxisID: 'y'
                }]
            },
            options: {
                responsive: true,
                scales: { y: { beginAtZero: true, max: 100, title: { display: true, text: 'Percentage (%)' } } }
            }
        });
    }

    function renderCardViabilityChart() {
        if (allReports.length === 0) return;
        const lastReport = allReports[allReports.length - 1].Report;
        const ctx = document.getElementById('cardViabilityChart').getContext('2d');

        const dataPoints = lastReport.CardViability.map(card => ({
            x: card.PickRate * 100,
            y: card.WinRateWhenPicked * 100,
            label: card.CardId
        }));
        
        charts.cardViability = new Chart(ctx, {
            type: 'scatter',
            data: {
                datasets: [{
                    label: 'Cards',
                    data: dataPoints,
                    backgroundColor: 'rgba(255, 99, 132, 0.6)'
                }]
            },
            options: {
                scales: {
                    x: {
                        type: 'linear',
                        position: 'bottom',
                        title: { display: true, text: 'Pick Rate (%)' }
                    },
                    y: {
                        title: { display: true, text: 'Win Rate When Picked (%)' }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const point = context.raw;
                                return `${point.label}: (Pick: ${point.x.toFixed(1)}%, Win: ${point.y.toFixed(1)}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    function populateCardSelector() {
        if (allReports.length === 0) return;
        const allCardIds = new Set();
        allReports.forEach(report => {
            report.Report.CardViability.forEach(card => allCardIds.add(card.CardId));
        });
        
        cardSelector.innerHTML = '';
        Array.from(allCardIds).sort().forEach(cardId => {
            const option = document.createElement('option');
            option.value = cardId;
            option.textContent = cardId;
            cardSelector.appendChild(option);
        });
    }
    
    function renderCardTrendChart() {
        const selectedId = cardSelector.value;
        if (!selectedId) return;

        const labels = allReports.map(r => `Gen ${r.Generation}`);
        const pickRates = [];
        const winRates = [];

        allReports.forEach(report => {
            const cardData = report.Report.CardViability.find(c => c.CardId === selectedId);
            pickRates.push(cardData ? cardData.PickRate * 100 : null);
            winRates.push(cardData ? cardData.WinRateWhenPicked * 100 : null);
        });
        
        const ctx = document.getElementById('cardTrendChart').getContext('2d');
        charts.cardTrend = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Pick Rate (%)',
                    data: pickRates,
                    borderColor: 'rgb(255, 159, 64)',
                    yAxisID: 'y',
                    spanGaps: true
                }, {
                    label: 'Win Rate When Picked (%)',
                    data: winRates,
                    borderColor: 'rgb(153, 102, 255)',
                    yAxisID: 'y',
                    spanGaps: true
                }]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: `Trends for: ${selectedId}` } },
                scales: { y: { beginAtZero: true, max: 100, title: { display: true, text: 'Percentage (%)' } } }
            }
        });
    }
});