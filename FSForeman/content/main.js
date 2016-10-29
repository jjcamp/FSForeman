var apiSystem = "/fsforeman/api/system";
var apiDuplicates = "/fsforeman/api/duplicates";
var apiLog = "/fsforeman/api/log";
var apiRoots = "/fsforeman/api/roots";
var apiIgnores = "/fsforeman/api/ignores";
var apiDuplicates = "/fsforeman/api/duplicates";

var id = document.getElementById.bind(document);

var xhr = function(method, uri, success, failure) {
    var req = new XMLHttpRequest();
    req.onreadystatechange = function() {
        if (this.readyState == 4) {
            if (this.status == 200)
                success(this.responseText)
            else if(failure)
                failure(this.status, this.statusText)
        }
    }
    req.open(method, uri, true);
    req.send();
}

var getSystemInfo = function() {
    xhr('GET', apiSystem, function(response) {
        var json = JSON.parse(response);
        id('system-running').innerHTML = "Running";
        id('system-memory').innerHTML = +json.memory.toFixed(2);
        id('system-threads').innerHTML = json.threads;
    }, function() {
        id('system-running').innerHTML = "Stopped";
        id('system-memory').innerHTML = "0";
        id('system-threads').innerHTML = "0";
    });
}

var getDuplicates = function() {
    xhr('GET', apiDuplicates, function(response) {
        var json = JSON.parse(response);
        var html = "<tr><th>Size</th><th>Files</th></tr>";
        for (var i in json) {
            html += "<tr><td>" + json[i].size + "</td><td>";
            for (var j in json[i].files) {
                html += json[i].files[j] + "<br />";
            }
            html += "</td></tr>";
        }
        id('duplicates').innerHTML = html;
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