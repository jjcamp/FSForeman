var apiSystem = "/fsforeman/api/system";
var apiLog = "/fsforeman/api/log";
var apiRoots = "/fsforeman/api/roots";
var apiIgnores = "/fsforeman/api/ignores";
var apiDuplicates = "/fsforeman/api/duplicates";

var id = document.getElementById.bind(document);

var xhr = function(method, uri, callback) {
    var req = new XMLHttpRequest();
    req.onreadystatechange = function() {
        if (this.readyState == 4 && this.status == 200)
            callback(this.responseText)
    }
    req.open(method, uri, true);
    req.send();
}

var getSystemInfo = function() {
    xhr('GET', apiSystem, function(response) {
        var json = JSON.parse(response);
        id('system-memory').innerHTML = +json.memory.toFixed(2);
        id('system-threads').innerHTML = json.threads;
    });
}

var getLog = function() {
    xhr('GET', apiLog, function(response) {
        id('log-text').value = response;
    });
}

var getRoots = function() {
    xhr('GET', apiRoots, function(response) {
        var json = JSON.parse(response);
        var html = "";
        for (var i in json) {
            html += "<option>" + json[i] + "</option>";
        }
        id('root-select').innerHTML = html;
    });
}

var removeRoot = function() {
    var box = id('root-select');
    var selected = box.options[box.selectedIndex].text.replace(/\\/g, '%5C');
    xhr('DELETE', apiRoots + "?dir=" + selected, getRoots);
}

var addRoot = function() {
    var textBox = id('root-add-text');
    var dir = textBox.value.replace(/\\/g, '%5C');
    xhr('POST', apiRoots + "?dir=" + dir, getRoots);
    textBox.value = "";
}

var getIgnores = function() {
    xhr('GET', apiIgnores, function(response) {
        var json = JSON.parse(response);
        var html = "";
        for (var i in json) {
            html += "<option>" + json[i] + "</option>";
        }
        id('ignore-select').innerHTML = html;
    });
}

var removeIgnore = function() {
    var box = id('ignore-select');
    var selected = box.options[box.selectedIndex].text.replace(/\\/g, '%5C');
    xhr('DELETE', apiIgnores + "?pattern=" + selected, getIgnores);
}

var addIgnore = function() {
    var textBox = id('ignore-add-text');
    var pattern = textBox.value.replace(/\\/g, '%5C');
    xhr('POST', apiIgnores + "?pattern=" + pattern, getIgnores);
    textBox.value = "";
}

var getDuplicates = function() {
    xhr('GET', apiDuplicates, null);
}

getSystemInfo();
setInterval(getSystemInfo, 5000);
getLog();
setInterval(getLog, 5000);
getRoots();
getIgnores();

window.onload = function() {
    var rootRemoveButton = id('root-remove-button');
    rootRemoveButton.type = 'button';
    rootRemoveButton.addEventListener('click', removeRoot, false);
    var rootAddButton = id('root-add-button');
    rootAddButton.type = 'button';
    rootAddButton.addEventListener('click', addRoot, false);
    var ignoreRemoveButton = id('ignore-remove-button');
    ignoreRemoveButton.type = 'button';
    ignoreRemoveButton.addEventListener('click', removeIgnore, false);
    var ignoreAddButton = id('ignore-add-button');
    ignoreAddButton.type = 'button';
    ignoreAddButton.addEventListener('click', addIgnore, false);
    var duplicatesButton = id('duplicates-button');
    duplicatesButton.type = 'button';
    duplicatesButton.addEventListener('click', getDuplicates, false);
}