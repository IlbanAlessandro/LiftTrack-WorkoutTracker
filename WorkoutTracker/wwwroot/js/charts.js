'use strict';
// Chart.js v4 — all Y axes auto-scale from min/max of actual data values.
// Chart instances are stored so they can be destroyed before re-render.

Chart.defaults.color = '#9ca3af';
Chart.defaults.borderColor = '#2e2e3a';
Chart.defaults.font.family = "'Segoe UI', system-ui, sans-serif";
Chart.defaults.font.size = 11;

const OG = '#f97316';
const OG_FILL = 'rgba(249,115,22,0.12)';
const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

let _pr = null, _freq = null, _dist = null, _dur = null;
const kill = r => { try { r?.destroy(); } catch(_) {} };

function autoScale(values) {
    const ns = values.map(Number).filter(n => !isNaN(n) && isFinite(n));
    if (!ns.length || ns.every(n => n === 0)) return { min: 0, max: 1 };
    const mn = Math.min(...ns), mx = Math.max(...ns);
    const pad = (mx - mn) * 0.15 || 1;
    return { min: Math.max(0, Math.floor(mn - pad)), max: Math.ceil(mx + pad) };
}

window.renderPrChart = function(labels, values) {
    kill(_pr);
    const ctx = document.getElementById('prChart');
    if (!ctx) return;
    _pr = new Chart(ctx, {
        type: 'line',
        data: { labels, datasets: [{ label:'Performance', data:values, borderColor:OG, backgroundColor:OG_FILL, borderWidth:2, pointBackgroundColor:OG, pointRadius:4, pointHoverRadius:6, fill:true, tension:0.3 }] },
        options: {
            responsive:true, maintainAspectRatio:true,
            plugins:{ legend:{ display:false } },
            scales:{
                x:{ grid:{color:'#2e2e3a'}, ticks:{maxTicksLimit:10} },
                y:{ ...autoScale(values), grid:{color:'#2e2e3a'} }
            }
        }
    });
};

window.renderFreqChart = function(data) {
    kill(_freq);
    const ctx = document.getElementById('freqChart');
    if (!ctx) return;
    _freq = new Chart(ctx, {
        type: 'bar',
        data: { labels:MONTHS, datasets:[{label:'Workouts',data,backgroundColor:OG,borderColor:OG,borderRadius:4}] },
        options: { responsive:true, maintainAspectRatio:true, plugins:{legend:{display:false}}, scales:{ x:{grid:{display:false}}, y:{...autoScale(data),grid:{color:'#2e2e3a'},ticks:{stepSize:1}} } }
    });
};

window.renderDistChart = function(data) {
    kill(_dist);
    const ctx = document.getElementById('distChart');
    if (!ctx) return;
    const ns = data.map(Number);
    _dist = new Chart(ctx, {
        type: 'bar',
        data: { labels:MONTHS, datasets:[{label:'Distance (km)',data:ns,backgroundColor:OG,borderColor:OG,borderRadius:4}] },
        options: { responsive:true, maintainAspectRatio:true, plugins:{legend:{display:false}}, scales:{ x:{grid:{display:false}}, y:{...autoScale(ns),grid:{color:'#2e2e3a'}} } }
    });
};

window.renderDurChart = function(data) {
    kill(_dur);
    const ctx = document.getElementById('durChart');
    if (!ctx) return;
    const ns = data.map(Number);
    _dur = new Chart(ctx, {
        type: 'bar',
        data: { labels:MONTHS, datasets:[{label:'Duration (min)',data:ns,backgroundColor:OG,borderColor:OG,borderRadius:4}] },
        options: { responsive:true, maintainAspectRatio:true, plugins:{legend:{display:false}}, scales:{ x:{grid:{display:false}}, y:{...autoScale(ns),grid:{color:'#2e2e3a'}} } }
    });
};
