var testIndex = -1;
var currentRepeat = -1;
var repeatCount = 10;

var output = [];
output.length = repeatCount;
for (var i = 0; i < output.length; i++) {
    output[i] = {};
}

function start() 
{
    window.setTimeout(reallyNext, 500);
}

function next() 
{
    window.setTimeout(reallyNext, 10);
}

function reallyNext() 
{
    ///document.getElementById("frameparent").innerHTML = "";
    ///document.getElementById("frameparent").innerHTML = "<iframe id='testframe'>";
    ///var testFrame = document.getElementById("testframe");
    testIndex++;
    if (testIndex < tests.length) {
        ///testFrame.contentDocument.open();
        document.write(testContents[testIndex]);
        ///testFrame.contentDocument.close();
    } else if (++currentRepeat < repeatCount) { 
        testIndex = 0;
        ///testFrame.contentDocument.open();
        document.write(testContents[testIndex]);
        ///testFrame.contentDocument.close();
    } else {
        finish();
        return false;///
    }
    return true;///
}

function recordResult(time)
{
    if (currentRepeat >= 0) // negative repeats are warmups
        output[currentRepeat][tests[testIndex]] = time;
    next();
}

function finish()
{
    var outputString = "{";
    outputString += '"v": "sunspider-0.9.1", ';
    for (var test in output[0]) {
        outputString += '"' + test + '":[';
        for (var i = 0; i < output.length; i++) {
             outputString += output[i][test] + ",";
        }
        outputString = outputString.substring(0, outputString.length - 1);
        outputString += "],";
    }
    outputString = outputString.substring(0, outputString.length - 1);
    outputString += "}";

    location = "results.html?" + encodeURI(outputString);
}
alert('Hello World!');
start();///
while(reallyNext());///
