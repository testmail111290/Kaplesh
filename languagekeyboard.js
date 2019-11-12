/* Copyright 2013 webdunia.net */
;
var edit_mode = 0;
var kbt;
var kbdlang;
var iphone = 0;
var baseuri = 'http://www.webdunia.net/';
var pany = 0;
var lp;
var lx = 'swf';
var aut = new Array("audio/mpeg;codecs=mp3", "audio/ogg;codecs=vorbis");
var aux = new Array('mp3', 'ogg');
var dbg = 0;
var lid = '';
var tid = '';
var au0, au1;
var aul = 0;
var auri;
var emsg = "Sorry. An error occured in playback.";
function auw() {
    return !!document.createElement('audio').canPlayType;
}
function aucfg() {
    pany = 0;
    if (auw()) {
        var au = document.getElementById('au0');
        if (!au)
            return;
        for (var x = 0; x < aut.length; x++) {
            var canPlay = au.canPlayType(aut[x]);
            if ((canPlay == "maybe") || (canPlay == "probably")) {
                pany = 1;
                lp = aut[x];
                lx = aux[x];
                break;
            }
        }
        if (dbg) $("#msg").append(x + '-' + canPlay + '<br>');
        if (pany) {
            au.addEventListener('error', function (e) { $("#msg").append = emsg; }, true);
            au.addEventListener("ended", function () { $(".au").css("border-color", "transparent"); /*$("img.auon").removeClass('auon');*/ }, false);
            var au = document.getElementById('au1');
            au.addEventListener("ended", function () { $(".au1").css("border-color", "transparent"); /*removeClass('auon');*/ }, false);
        }
    }
}
function setMsg(type, text) {
    $("#msg").html("<p class=" + type + ">" + text + "</p>");
}
if ((navigator.userAgent.match(/iPhone/i)) || (navigator.userAgent.match(/iPod/i))) {
    //if (document.cookie.indexOf("iphone_redirect=false") == -1) window.location = "http://shabdkosh.mobi/?iphone&amp;i=COMR";
    iphone = 1;
}
/* Cross-platform search addin installer (FF or IE) */
function installSearchAddin() {
    if ("external" in window) {
        window.external.AddSearchProvider('http://cache.shabdkosh.com/searchAddin.xml');
    } else {
        //TODO: try another form of search install
        alert("Sorry, your browser does not support installing this Search Addin");
    }
}
;
var hi_rom_kbd = new Array(" ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*", "+", ",", "_", "॥", "्", "०", "१", "२", "३", "४", "५", "६", "७", "८", "९", ":", ";", "ङ", "=", "।", "?", "@", "आ", "भ", "च", "ध", "ै", "ऊ", "घ", "अ", "ी", "झ", "ख", "ळ", "ं", "ण", "ओ", "फ", "ठ", "ृ", "श", "थ", "ू", "ँ", "औ", "ढ", "ञ", "ऋ", "इ", "ॐ°", "ए", "^", "॒", "ऽ", "ा", "ब", "छ", "द", "े", "उ", "ग", "ह", "ि", "ज", "क", "ल", "म", "न", "ो", "प", "ट", "र", "स", "त", "ु", "व", "ौ", "ड", "य", "ष", "ई", "ः", "ऐ", "़");
var eng_kbd = new Array(" ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*", "+", ",", "-", ".", "/", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ":", ";", "<", "=", ">", "?", "@", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "[", "\\", "]", "^", "_", "`", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "{", "|", "}", "~");
//var rem_kbd = new Array(" ","।","ष्‍", ":","*",  "-","'", "श्‍","त्र","ऋ‍","द्ध","्","ए",";","ण्‍","ध्‍","०","१","२","३","४","५","६","७","८","९","रू‍","य","ढ","ृ","झ","घ्‍","/","ा‍","ठ","ब्‍","क्‍‍","म्‍‍","थ्‍","ळ‍","भ्‍‍","प्‍‍","श्र‍","ज्ञ‍","स्‍","ड","छ‍","व्‍","च्‍","फ","त्‍‍","ै‍","ज्‍‍","न्‍‍","ट‍","ॅ‍","ग्‍","ल्‍‍","र्‍","ख्‍","(",",","'",".","़","ं","इ","ब","क","म","ि‍","ह","ी","प‍","र","ा","स","उ","द","व","च","ु","त","े","ज","न","अ","ू","ग","ल","्र","क्ष्‍",")","द्व","द्य‍");
//var ins_kbd = new Array(" ","ऍ","ठ","्र","र्","ज्ञ","क्ष","ट","(",")","श्र","ऋ",",","-",".","य","०","१","२","३","४","५","६","७","८","९","छ","च","ष","ृ","।","य़","ॅ","ओ","ऴ","ण","अ","आ","इ","उ","फ","घ","ऱ","ख","थ","श","ळ","ध","झ","औ","ई","ए","ऊ","ङ","ऩ","ऐ","ँ","भ"," ","ड","ॉ","़","त्र","ः","'","ो","व","म","्","ा","ि","ु","प","ग","र","क","त","स","ल","द","ज","ौ","ी","े","ू","ह","न","ै","ं","ब"," ","ढ","ऑ","ञ","~" );
var kbd_types = new Array('eng_kbd', 'ins_kbd', 'rom_kbd'); //,'rem_kbd','ang_kbd','trn_kbd');
//var rem_xtra = new Array("ौ","ो");
var bn_ins_kbd = new Array(" ", "", "ঠ", "্র", "র্", "জ্ঞ", "ক্ষ", "ট", "ৢ", "ৣ", "শ্র", "ঋ", "ৡ", "-", "ৠ", "ষ়", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "ছ", "চ", "ষ", "ৃ", "।", "ষ", "", "ও", "ঌ", "ণ", "অ", "আ", "ই", "উ", "ফ", "ঘ", "", "খ", "থ", "শ", "", "ধ", "ঝ", "ঔ", "ঈ", "এ", "ঊ", "ঙ", "য়", "ঐ", "ঁ", "ভ", "", "ড", "", "়", "ত্র", "ঃ", "", "ো", "ব", "ম", "্", "া", "ি", "ু", "প", "গ", "র", "ক", "ত", "স", "ল", "দ", "জ", "ৌ", "ী", "ে", "ূ", "হ", "ন", "ৈ", "ং", "ব", "ৗ", "ঢ", "", "ঞ", "", "-");
//var bn_ins_kbd = new Array(" ", "", "ঠ", "্র", "র্", "জ্ঞ", "ক্ষ", "ট", "", "", "শ্র", "ঋ", "", "-", "", "ষ়", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "ছ", "চ", "ষ", "ৃ", "।", "ষ", "", "ও", "", "ণ", "অ", "আ", "ই", "উ", "ফ", "ঘ", "", "খ", "থ", "শ", "", "ধ", "ঝ", "ঔ", "ঈ", "এ", "ঊ", "ঙ", "", "ঐ", "ঁ", "ভ", "", "ড", "", "়", "ত্র", "ঃ", "", "ো", "ব", "ম", "্", "া", "ি", "ু", "প", "গ", "র", "ক", "ত", "স", "ল", "দ", "জ", "ৌ", "ী", "ে", "ূ", "হ", "ন", "ৈ", "ং", "ব", "", "ঢ", "", "ঞ", "", "-");
//var bn_ins_kbd = new Array(" ", "", "ঠ", " ্র ", "র্", "জ্ঞ", "ক্ষ", "ট", "ৢ", " ৣ", "শ্র", "ঋ", "ৡ", "-", "ৠ", "ষ়", "০", "১", "২", "৩", "৪", "৫", "৬", "৭", "৮", "৯", "ছ", "চ", "ষ", " ৃ ", "।", "ষ", "", "ও", "ঌ", "ণ", "অ", "আ", "ই", "উ", "ফ", "ঘ", "", "খ", "থ", "শ", "", "ধ", "ঝ", "ঔ", "ঈ", "এ", "ঊ", "ঙ", "য়", "ঐ", " ঁ ", "ভ", "", "ড", "", " ় ", "ত্র", " ঃ ", "", " ো ", "ব", "ম", " ্ ", " া ", " ি ", " ু ", "প", "গ", "র", "ক", "ত", "স", "ল", "দ", "জ", " ৌ ", " ী ", " ে ", " ূ ", "হ", "ন", " ৈ ", " ং ", "ব", "ৗ", "ঢ", "", "ঞ", "", "-");
var gu_ins_kbd = new Array(" ", "ઍ", "ઠ", "્ર", "ર્", "જ્ઞ", "ક્ષ", "ટ", "(", ")", "શ્ર", "ઋ", ",", "-", ".", "ય", "૦", "૧", "૨", "૩", "૪", "૫", "૬", "૭", "૮", "૯", "છ", "ચ", "ષ", "ૃ", "|", "?", "ૅ", "ઓ", "’", "ણ", "અ", "આ", "ઇ", "ઉ", "ફ", "ઘ", "", "ખ", "થ", "શ", "ળ", "ધ", "ઝ", "ઔ", "ઈ", "એ", "ઊ", "ઙ", "‘", "ઐ", "ઁ", "ભ", "", "ડ", "ૉ", "઼", "ત્ર", "ઃ", "’", "ો", "વ", "મ", "્", "ા", "િ", "ુ", "પ", "ગ", "ર", "ક", "ત", "સ", "લ", "દ", "જ", "ૌ", "ી", "ે", "ૂ", "હ", "ન", "ૈ", "ં", "બ", "", "ઢ", "ઑ", "ઞ", "~", "-");
var hi_ins_kbd = new Array(" ", "ऍ", "ठ", "्र", "र्", "ज्ञ", "क्ष", "ट", "(", ")", "श्र", "ऋ", ",", "-", ".", "य", "०", "१", "२", "३", "४", "५", "६", "७", "८", "९", "छ", "च", "ष", "ृ", "।", "?", "ॅ", "ओ", "", "ण", "अ", "आ", "इ", "उ", "फ", "घ", "ऱ", "ख", "थ", "श", "ळ", "ध", "झ", "औ", "ई", "ए", "ऊ", "ङ", "", "ऐ", "ँ", "भ", "", "ड", "ॉ", "़", "त्र", "ः", "'", "ो", "व", "म", "्", "ा", "ि", "ु", "प", "ग", "र", "क", "त", "स", "ल", "द", "ज", "ौ", "ी", "े", "ू", "ह", "न", "ै", "ं", "ब", "", "ढ", "ऑ", "ञ", "~", "-", "");
var kn_ins_kbd = new Array(" ", "", "ಠ", "್ರ", "ರ್", "ಜ್ಞ", "ಕ್ಷ", "ಟ", "(", ")", "ಶ್ರ", "ಋ", ",", "-", ".", "ಯ", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "ಛ", "ಚ", "", "ೃ", "", "?", "", "ಓ", "", "ಣ", "ಅ", "ಆ", "ಇ", "ಉ", "ಫ", "ಘ", "ಱ", "ಖ", "ಥ", "ಶ", "ಳ", "ಧ", "ಝ", "ಔ", "ಈ", "ಏ", "ಊ", "ಙ", "", "ಐ", "", "ಭ", "ಎ", "ಡ", "", "", "ತ್ರ", "ಃ", "ೊ", "ೋ", "ವ", "ಮ", "್", "ಾ", "ಿ", "ು", "ಪ", "ಗ", "ರ", "ಕ", "ತ", "ಸ", "ಲ", "ದ", "ಜ", "ೌ", "ೀ", "ೇ", "ೂ", "ಹ", "ನ", "ೈ", "ಂ", "ಬ", "ೆ", "ಢ", "", "ಞ", "ಒ", "-");
var mr_ins_kbd = new Array(" ", "ऍ", "ठ", "्र", "र्", "ज्ञ", "क्ष", "ट", "(", ")", "श्र", "ऋ", ",", "-", ".", "य", "०", "१", "२", "३", "४", "५", "६", "७", "८", "९", "छ", "च", "ष", "ृ", "।", "?", "ॅ", "ओ", "", "ण", "अ", "आ", "इ", "उ", "फ", "घ", "ऱ", "ख", "थ", "श", "ळ", "ध", "झ", "औ", "ई", "ए", "ऊ", "ङ", "", "ऐ", "ँ", "भ", "", "ड", "ॉ", "़", "त्र", "ः", "'", "ो", "व", "म", "्", "ा", "ि", "ु", "प", "ग", "र", "क", "त", "स", "ल", "द", "ज", "ौ", "ी", "े", "ू", "ह", "न", "ै", "ं", "ब", "", "ढ", "ऑ", "ञ", "~", "-");
var ml_ins_kbd = new Array(" ", "!", "ഠ", "്ര", ":", ";", "ക്ഷ", "ട", "(", ")", "*", "ഋ", ",", "-", ".", "യ", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "ഛ", "ച", "ഷ", "ൃ", "•", "?", "‘", "ഓ", "ള", "ണ", "അ", "ആ", "ഇ", "ഉ", "ഫ", "ഘ", "റ", "ഖ", "ഥ", "ശ", "ഴ", "ധ", "ഝ", "ഔ", "ഈ", "ഏ", "ഊ", "ങ", "ഃ", "ഐ", "", "ഭ", "എ", "ഡ", "", "", "’", "ഃ", "ൊ", "ോ", "വ", "മ", "്", "ാ", "ി", "ു", "പ", "ഗ", "ര", "ക", "ത", "സ", "ല", "ദ", "ജ", "ൗ", "ീ", "േ", "ൂ", "ഹ", "ന", "ൈ", "ം", "ബ", "െ", "ഢ", "", "ഞ", "ഒ", "-");
var pa_ins_kbd = new Array(" ", "!", "ਠ", "੍ਰ", "", "%", "", "ਟ", "(", ")", "*", "+", "", "-", "", "ਯ", "੦", "੧", "੨", "੩", "੪", "੫", "੬", "੭", "੮", "੯", "ਛ", "ਚ", "", "=", "", "?", "", "ਓ", "", "ਣ", "ਅ", "ਆ", "ਇ", "ਉ", "ਫ", "ਘ", "", "ਖ", "ਥ", "ਸ਼", "", "ਧ", "ਝ", "ਔ", "ਈ", "ਏ", "ਊ", "ਙ", "", "ਐ", "", "ਭ", "", "ਡ", "", "਼", "", "_", "", "ੋ", "ਵ", "ਮ", "੍", "ਾ", "ਿ", "ੁ", "ਪ", "ਗ", "ਰ", "ਕ", "ਤ", "ਸ", "ਲ", "ਦ", "ਜ", "ੌ", "ੀ", "ੇ", "ੂ", "ਹ", "ਨ", "ੈ", "ਂ", "ਬ", "", "ਢ", "|", "ਞ", "", "-");
var ta_ins_kbd = new Array(" ", "", "ட", "", "", "", "க்ஷ", "ட", "", "", "வ்ர", "", ",", "", ".", "ய", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "ச", "ச", "ஷ", "", "", "ய", "", "ஓ", "ழ", "ண", "அ", "ஆ", "இ", "உ", "ப", "க", "ற", "க", "த", "ஷ", "ள", "த", "ச", "ஔ", "ஈ", "ஏ", "ஊ", "ங", "ன", "ஐ", "", "ப", "எ", "ட", "", "", "", "ஃ", "ொ", "ோ", "வ", "ம", "்", "ா", "ி", "ு", "ப", "க", "ர", "க", "த", "ஸ", "ல", "த", "ஜ", "ௌ", "ீ", "ே", "ூ", "ஹ", "ந", "ை", "", "ப", "ெ", "ட", "", "ஞ", "ஒ", "");
var te_ins_kbd = new Array(" ", "", "ఠ", "్ర", "", "జ్ఞ", "క్ష", "శ్ర", "(", ")", "శ్ర", "ఋ", ",", "-", ".", "య", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "ఛ", "చ", "ష", "ృ", "|", "", "", "ఓ", "", "ణ", "", "అ", "ఆ", "ఇ", "ఉ", "ఫ", "ఘ", "ఱ", "ఖ", "థ", "శ", "ళ", "ధ", "ఝ", "ఔ", "ఈ", "ఏ", "ఊ", "ఙ", "", "ఐ", "ఁ", "భ", "ఎ", "డ", "య", "య", "త్ర", "ః", "ొ", "ో", "వ", "మ", "మ", "్", "ా", "ి", "ు", "ప", "గ", "ర", "క", "త", "స", "ల", "ద", "జ", "ౌ", "ీ", "ే", "ూ", "హ", "న", "ై", "ం", "బ", "ె", "ఢ", "య", "య", "ఒ", "-");
var row00 = new Array(96, 33, 64, 35, 36, 37, 94, 38, 42, 40, 41, 95, 43);
var row01 = new Array(130, 39, 139, 140, 160, 141, 161, 142, 162, 132, 152, 131, 43);
var row10 = new Array(96, 49, 50, 51, 52, 53, 54, 55, 56, 57, 48, 45, 61); // 13 keys + BS
var row11 = new Array(126, 33, 64, 35, 36, 37, 94, 38, 42, 40, 41, 95, 43);
var row20 = new Array(113, 119, 101, 114, 116, 121, 117, 105, 111, 112, 91, 93, 92); // Tab + 14
var row21 = new Array(81, 87, 69, 82, 84, 89, 85, 73, 79, 80, 123, 125, 124);
var row30 = new Array(97, 115, 100, 102, 103, 104, 106, 107, 108, 59, 39); // CAPS + 11 + CRLF
var row31 = new Array(65, 83, 68, 70, 71, 72, 74, 75, 76, 58, 34);
var row40 = new Array(122, 120, 99, 118, 98, 110, 109, 44, 46, 47); // LShift + 10 + RShift
var row41 = new Array(90, 88, 67, 86, 66, 78, 77, 60, 62, 63);
var sb = new Array();
var lastCode = '';
function dk(id, f, e) {
    var code;
    var kc;
    var t;
    var kbd;
    if (kbt == 'eng_kbd')
        return true;
    kbd = kbdlang + '_' + kbt;
    if (window.event) // IE
        kc = window.event.keyCode;
    else if (e.which) // FF
        kc = e.which;
    if (kc == 13) return true;
    if (kc == undefined) return true;
    if (kc == 8) return true;
    code = eval(kbd + '[' + kc + '-32]');
    lastCode = kc;
    t = el(f);
    t.value += code;
    return false;
}
function tabClick(f) {
    var t = el(f);
    var strPos = getCaret(t);
    var front = (t.value).substring(0, strPos);
    var back = (t.value).substring(strPos, t.value.length);
    t.value = front + "    " + back;
    strPos = strPos + "    ".length;
    setCaretToPos(t, strPos);
    //t.selectionStart = t.selectionEnd = strPos + 1;
    this.preventDefault();
    if (!iphone) t.focus();
}
function deleteLast(f) {
    var t = el(f);
    var currPos = getCaret(t);
    var cval = t.value;
    var afterVal = cval.substring(currPos, cval.length);
    if (currPos > 0) {
        cval = cval.substring(0, currPos - 1);
        t.value = cval + afterVal;
        setCaretToPos(t, currPos - 1);
    }
    if (!iphone) t.focus();
    /* Old code working for last keyword only
    var t = el(f);
    var cval = t.value;
    var clen = cval.length;
    cval = cval.substring(0, clen - 1);
    t.value = cval;
    if (!iphone) t.focus();
    */
}
function appendText(f, c) {
    var t = el(f);
    //t.value = t.value + c;

    var strPos = getCaret(t);
    
    var front = (t.value).substring(0, strPos);
    var back = (t.value).substring(strPos, t.value.length);
    t.value = front + c + back;
    strPos = strPos + c.length;
    setCaretToPos(t, strPos);

    if (!iphone) t.focus();
}
function drawKbd(element, id, e, k) {
    //    alert(element + ' and id = ' + id);
    //    alert(el(element));
    el(element).innerHTML += getKbd(id, e, k);
}
// id - keyboard element index on page (typically zero)
// e  - element id that holds search entry
// k  - element id that holds keyboard config
function getKbd(id, f, s) {
    //alert('getKbd called first');
    var i, j, code, tgt, kbd, kbd_id;
    var r = '';
    r += '<div class="yui3-g"><div class="yui3-u-3-5" id="kbd' + id + '">';
    if (kbt != "eng_kbd")
        kbd = kbdlang + '_' + kbt; //s[kbd_id];
    else
        kbd = kbt;
    r += '<div class="keyrow">';
    for (i = 0; i < row10.length; i = i + 1) {
        j = row10[i] - 32;
        code = eval(kbd + '[' + j + ']');
        r += ak("hkey", id + '1' + i, code, f);
    }
    r += '<input class="hkeybig" type="button" name="backspace" title="Backspace" value="Back" onClick="deleteLast(\'' + f + '\');">';
    r += '<br/>';
    r += '</div>';
    r += '<div class="keyrow">';
    r += '<input class="hkeybig" type="button" name="tab" title="Tab" value="Tab" onClick="tabClick(\'' + f + '\');">';
    for (i = 0; i < row20.length; i = i + 1) {
        j = row20[i] - 32;
        code = eval(kbd + '[' + j + ']');
        r += ak("hkey", id + '2' + i, code, f);
    }
    r += '<br/>';
    r += '</div>';
    r += '<div class="keyrow">';
    r += '<input class="hkeybig" type="button" name="CAPS" title="Caps Lock" value="Caps">';
    for (i = 0; i < row30.length; i = i + 1) {
        j = row30[i] - 32;
        code = eval(kbd + '[' + j + ']');
        r += ak("hkey", id + '3' + i, code, f);
    }
    r += '<input class="hkeyenter" type="button" name="enter" title="Insert line break" value="Enter" onClick="appentNewline(\'' + f + '\');">';
    r += '<br/>';
    r += '</div>';
    r += '<div class="keyrow">';
    r += '<input class="hkeyshift" type="button" name="shift" title="Change Shift Mode" value="Shift">';
    for (i = 0; i < row40.length; i = i + 1) {
        j = row40[i] - 32;
        code = eval(kbd + '[' + j + ']');
        r += ak("hkey", id + '4' + i, code, f);
    }
    r += '<input class="hkeyshift" type="button" name="shift" title="Change Shift Mode" value="Shift">';
    r += '</div><div><input class="hkeyspace" type="button" name="space" title="Space Bar" value="Space" onClick="appentSpaceBar(\'' + f + '\');"><div>';
    r += '</div><div id="kbdhelp" class="yui3-u-2-5">&nbsp;</div></div>';
    return r;
}
function appentNewline(f) {
    appendText(f, '\n');
}
function appentSpaceBar(f) {
    appendText(f, ' ');
}
function al(mode) {
    var row, i, j, l, n, mode, keyname;
    var id = 0;
    var kbdindex = parseInt(el("k").value, 10) & 0xf;
    var kbdname = kbd_types[kbdindex];
}
function handleKeyDown(id, f, e) {
    if (el('kbd_mode' + id).value == "sh")
        return;
    if (!e)
        e = window.event;
    if (e.shiftKey) {
        el('kbd_mode' + id).value = "sh";
        al(id, el('kbd_layout' + id).value);
    }
}
function handleKeyUp(id, f, e) {
    if (!e)
        e = window.event;
    if (!e.shiftKey) {
        el('kbd_mode' + id).value = "st";
        al(id, el('kbd_layout' + id).value);
    }
}
function change_kbd_mode(kbd, mode) {
    kbdlang = $("#l").val();
    var row, i, j, l, n, mode, keyname;
    var id = 0;
    //    if (kbd != "eng_kbd")
    kbd = kbdlang + '_' + kbd;
    for (n = 1; n <= 4; n = n + 1) {
        row = 'row' + n + String(mode);
        l = eval(row + '.length');
        for (i = 0; i < l; i = i + 1) {
            j = eval(row + '[' + i + ']') - 32;
            code = eval(kbd + '[' + j + ']');
            keyname = 'key' + id + n + i;
            el(keyname).value = code;
        }
    }
}
function updateState(id, s) {
    var state_value = el(s).value;
    var x = state_value >> 7;
    var y = el('kbd_layout' + id).selectedIndex;
    var z = state_value & 0x3;
    el(s).value = (x << 7) | (y << 2) | z;
    el('state' + id).innerHTML = el(s).value;
}
function el(id) { if (document.getElementById) return document.getElementById(id); return null; }
function dw(x) { document.write(x); return true; }
function ak(c, id, code, f) {
    var r = '<input class="' + c + '" type="button" id="key' + id + '" name="hkey" value="' + code + '" onClick="appendText(\'' + f + '\',this.value);">';
    return r;
}
function fbck(a) {
    return '<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,40,0" WIDTH="0" HEIGHT="0" id="skaudio"><PARAM NAME=movie VALUE="' + a + '"><PARAM NAME=quality VALUE=high><PARAM NAME=bgcolor VALUE=#FFFFFF><param name=loop value=false><EMBED src="' + a + '" quality=high bgcolor=#FFFFFF WIDTH="0" HEIGHT="0" ALIGN="" loop="false" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED></OBJECT>';
}
;
//$(document).ready(function () {
function documentReady() {
    //alert('document ready called');
    var lcode = $("#lcode").val();
    var lname = $("#lname").val();
    aucfg();
    kbdlang = "hi";
    if (kbdlang == "hi")
        $("#rom_kbd").show();
    //    var eac_options = $('#e').autocomplete({
    //        serviceUrl: '/utils/autocomplete.php',
    //        minChars: 1,
    //        delimiter: /(,|;)\s*/, // regex or character
    //        maxHeight: 400,
    //        width: 300,
    //        zIndex: 9999,
    //        deferRequestBy: 10, //miliseconds
    //        params: { lc: $("#l").val() }, //aditional parameters
    //        noCache: false, //default is false, set to true to disable caching
    //        // callback function:
    //        onSelect: function (value, data) { $("#dict_search_form").submit(); }
    //    });
    $("#txtOther").focus().select();
    $("#t_on").click(function () { cookie('exp_t', 1); $("#dict_search_form").submit(); return false; });
    $("#t_off").click(function () { cookie('exp_t', null); $("#dict_search_form").submit(); return false; });
    /*
    $("#xlit").click(function(){
    $("#e").toggleClass("transliterate");
    if ($("#xlit").val() == "hindi"){
    $("#xlit").val('English');
    $("#f").val(0);
    $("#e").setOptions({ enable_xlit: false});
    } else {
    $("#xlit").val('hindi');
    $("#f").val(1);
    $("#e").setOptions({ enable_xlit: true});
    }
    $("#e").focus();
    });
    */
    var active_color = '#EFF1FB';
    // Insert play link
    $(".au").click(function () {
        tid = $(this).attr('id');
        au = document.getElementById('au0');
        auri = baseuri + tid + '&x=' + lx;
        if (pany) {
            $(this).css("border-color", "green");
            if (aul == 0) {
                au.src = auri;
                au.load();
                aul = 1;
            }
            au.play();
        }
        else {
            if (dbg) $("#msg").append('Fallback!<br>' + auri + '<br>');
            $('#audio').html(fbck(auri));
        }
    });
    $(".au1").click(function () {
        tid = $(this).attr('id');
        au = document.getElementById('au1');
        auri = baseuri + tid + '&x=' + lx;
        if (pany) {
            $(this).css("border-color", "green");
            if (lid != tid) {
                $('#au1').attr('src', auri);
                au.load();
            }
            au.play();
            lid = tid;
        }
        else {
            if (dbg) $("#msg").append('Fallback!<br>');
            $("#audio").html(fbck(auri));
        }
    });
    $(".au").attr("title", "Speak!");
    $(".au1").attr("title", "Speak!");
    // Insert reverse search link
    var lpath = (kbdlang == 'hi') ? '' : '/' + kbdlang;
    $(".l").click(function () {
        var e = $(this).html().replace(/\(|\)/g, ''); // remove anything in parentheses
        $("#txtOther").val(e);
        $("#dict_search_form").submit();
    });
    $(".l").attr("title", "Search this word/phrase");
    $("ol.oledit>li").hover(function () {
        var xid = $(this).attr('id');
        var x = xid.split('_');
        var dir = x[1];
        var sid = x[2];
        var tid = x[3];
        var sl = x[4];
        var tl = x[5];
        var pos = x[6];
        var ing = x[7];
        var l = $("#l").val();
        $(this).append($('<button type="button" value="DEL" class="delete" title="Delete this meaning">Delete</button>'));
        $(this).append($('<button type="button" value="EDIT" class="edit" title="Send this for review">Edit</button>'));
        $("button.delete").click(function () {
            delete_entry(l, dir, sid, tid, pos, ing, sl, tl, xid);
        });
        $("button.edit").click(function () {
            //alert("ing:"+ing+", pos:"+pos+", el:"+el+", il:"+il);
            var p = $(this).position();
            edit_entry(l, dir, sid, tid, sl, tl, ing, pos, p.left, p.top);
        });
    }, function () {
        $(this).find("button:last").remove();
        $(this).find("button:last").remove();
    });
    $("#contrib_clear").click(function () { clear_cform(); });
    $("#edit_help").children().hide();
    $("#en_help").show();
    // Enable the form
    $("#contrib_form").children("#contrib_en").focus(function () {
        $("#edit_help").children().hide();
        $("#en_help").show();
    });
    $("#contrib_form").children("#contrib_entags").focus(function () {
        $("#edit_help").children().hide();
        $("#engrammar").show();
    });
    $("#contrib_form").children("#contrib_ind").focus(function () {
        $("#edit_help").children().hide();
        $("#ind_help").show();
    });
    $("#contrib_form").children("#contrib_indtags").focus(function () {
        $("#edit_help").children().hide();
        $("#higrammar").show();
    });
    $("#contrib_entags").keypress(function () {
        //alert("Please use the options provided on the right side to add tags");
        return false;
    });
    $("#contrib_indtags").keypress(function () {
        //alert("Use the options provided on the right side to add tags");
        return false;
    });
    $("input#contrib_en").focus(function () { $(this).select(); });
    $("input#contrib_ind").focus(function () { $(this).select(); });
    //Show and hide synsets
    $("#syntask").click(function () { $(this).prev("#syn_overflow").slideToggle(); });
    $("#syntask").toggle(function () { $(this).html(" &laquo; See Less"); }, function () { $(this).html("See More &raquo;"); });
    // Keyboard Related
    var shiftmode = 0;
    kbt = cookie('kbt');
    if (!kbt)
        kbt = "eng_kbd";
    if ((kbt == 'rom_kbd') && (kbdlang != 'hi'))
        kbt = "eng_kbd";
    kbdshow = cookie('kbdshow');
    if (!kbdshow)
        kbdshow = 0;
    if (kbdshow == 1) {
        $("#keyboard").css("display", "block");
        $("#kbdsnh").addClass("activekbd");
    }
    if (kbt == "ins_kbd") $("#ins_kbd").addClass("activexlt");
    if (kbt == "rom_kbd") $("#rom_kbd").addClass("activexlt");
    drawKbd("keyboard", 0, "txtOther", "k");
    //$("#kbdhelp").html("<p>Use on-screen keyboard to enter text.<ol><li>Note that 'matra' is added after the consonant.</li><li>To get half characters, use halant which is mapped to 'd' key in INSCRIPT keyboard and to the '/' key in Romanized keyboard.</li></ol></p>");
    $("#kbdhelp").html("");
    $("#ins_kbd").change(function () {
        kbt = 'ins_kbd';
        //var drplang = $("#l").val();
        cookie('kbt', kbt);
        $(this).removeClass("activexlt");
        change_kbd_mode(kbt, shiftmode);
        $("#keyboard").slideToggle("slow");
        if (!iphone) $("#txtOther").focus();
    });
    $("#rom_kbd").click(function () {
        kbt = (kbt == 'rom_kbd') ? 'eng_kbd' : 'rom_kbd';
        //    cookie('kbt',kbt);
        $(this).toggleClass("activexlt").siblings().removeClass("activexlt");
        change_kbd_mode(kbt, shiftmode);
        if (!iphone) $("#txtOther").focus();
    });
    $(".hkeyshift").click(function () {
        shiftmode ^= 1;
        change_kbd_mode(kbt, shiftmode);
        $(".hkeyshift").toggleClass("shiftactive");
    });    
    $("#txtOther").keyup(function (e) {
        if (e.keyCode == 16) {
            shiftmode = 0;
            change_kbd_mode(kbt, shiftmode);
            $(".hkeyshift").removeClass("shiftactive");
        }
    });
    $("#txtOther").keydown(function (e) {
        if (e.keyCode == 16) {
            shiftmode = 1;
            change_kbd_mode(kbt, shiftmode);
            $(".hkeyshift").addClass("shiftactive");
        }
    });
    $("#txtOther").keypress(function (e) {
        return dk(0, this.id, e);
    });
    //    $("#kbdsnh").click(function () {
    //        kbdshow = 1;
    //        $("#keyboard").slideToggle("slow");
    //        $("#kbdsnh").toggleClass("activekbd");
    //        if (!iphone) {
    //            $("#e").focus();
    //        }
    //        cookie('kbdshow', kbdshow);
    //    });
    //    $("Select#drpLanguage").change(function () {
    //        cookie('kbt', 'eng_kbd');
    //        $("#ins_kbd").removeClass("activexlt");
    //    });
    /*
    $("#rapp").click(function(){     
    var tsx = new Date().getTime();
    $("#rstatus").show();
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=70&l="+$("#l").val()+"&eid="+$("#reng").attr('eid')+"&oid="+$("#reng").attr('oid')+"&tsx="+tsx+'&v=1',
    dataType:"json",
    success: function(response){
    $("#reng").attr("eid", response.reid);
    $("#reng").attr("oid", response.roid);
    $("#reng").text(response.reng);
    $("#rgra").text(response.rgra);
    $("#rind").text(response.rind);
    $("#rstatus").hide();
    }
    });
    return false;
    });
    */
    /*
    $("#rrej").click(function(){
    var tsx = new Date().getTime();
    $("#rstatus").show();
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=70&l="+$("#l").val()+"&eid="+$("#reng").attr('eid')+"&oid="+$("#reng").attr('oid')+"&tsx="+tsx+'&v=-1',
    dataType:"json",
    success: function(response){
    $("#reng").attr("eid", response.reid);
    $("#reng").attr("oid", response.roid);
    $("#reng").text(response.reng);
    $("#rgra").text(response.rgra);
    $("#rind").text(response.rind);
    $("#rstatus").hide();
    }
    });
    return false;
    });
    */
    /*
    $("#rski").click(function(){
    var tsx = new Date().getTime();
    $("#rstatus").show();
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=71&l="+$("#l").val()+"&eid="+$("#reng").attr('eid')+"&oid="+$("#reng").attr('oid')+"&tsx="+tsx+'&v=0',
    dataType:"json",
    success: function(response){
    $("#reng").attr("eid", response.reid);
    $("#reng").attr("oid", response.roid);
    $("#reng").text(response.reng);
    $("#rgra").text(response.rgra);
    $("#rind").text(response.rind);
    $("#rstatus").hide();
    }
    });
    return false;
    });
    */
    /*
    $("#sesub").click(function(){
    var tsx = new Date().getTime();
    $("#sestatus").show();
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=80&l="+$("#l").val()+"&en="+$("#seeng").val()+"&eid="+$("#seind").attr("nfid")+"&count="+$("#secnt").val()+"&ind="+$("#seind").text()+"&tsx="+tsx,
    dataType:"json",
    success: function(response){
    $("#seeng").val('');
    $("#seind").text(response.ind);
    $("#seind").attr("nfid",response.nfid);
    $("#sestatus").hide();
    }
    });
    return false;
    });
    */
    /*
    $("#sisub").click(function(){
    $("#simsg").removeClass();
    $("#simsg").empty();
    var engok = is_english($("#sieng").text());
    if (!engok){
    $("#sieng").css("border", "solid 1px red");
    $("#simsg").append("Please enter English only.<br/>");
    $("#simsg").addClass("warning");
    }
    var indok = is_indic($("#siind").val());
    if (!indok){
    $("#siind").css("border", "solid 1px red");
    $("#simsg").append("Please enter "+lname+" only.<br/>").addClass("warning");
    }
    if (indok && engok) {
    var tsx = new Date().getTime();
    $("#sistatus").show();
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=82&l="+$("#l").val()+"&en="+$("#sieng").text()+"&eid="+$("#sieng").attr("nfid")+"&count="+$("#sicnt").val()+"&ind="+$("#siind").val()+"&tsx="+tsx,
    dataType:"json",
    success: function(response){
    $("#siind").val('');
    $("#sieng").text(response.eng);
    $("#sieng").attr("nfid",response.nfid);
    $("#sistatus").hide();
    }
    });
    }
    return false;
    });
    */
    /*
    $("#seski").click(function(){
    var tsx = new Date().getTime();
    $("#sestatus").show();
    //alert($("#seind").attr('nfid'));
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=81&l="+$("#l").val()+"&eid="+$("#seind").attr("nfid")+"&tsx="+tsx,
    dataType:"json",
    success: function(response){
    $("#seeng").val('');
    $("#seind").text(response.ind);
    $("#seind").attr("nfid",response.nfid);
    $("#sestatus").hide();
    }
    });
    return false;
    });
    */
    /*
    $("#siski").click(function(){
    var tsx = new Date().getTime();
    $("#sistatus").show();
    $("#simsg").removeClass().empty();
    //alert($("#sieng").attr('nfid'));
    $.ajax({
    type: "GET",
    url: "/contribute/sktasks",
    data: "t=83&l="+$("#l").val()+"&eid="+$("#sieng").attr("nfid")+"&tsx="+tsx,
    dataType:"json",
    success: function(response){
    $("#siind").val('');
    $("#sieng").text(response.eng);
    $("#sieng").attr("nfid",response.nfid);
    $("#sistatus").hide();
    }
    });
    return false;
    });
    */
    function is_english(value) {
        var r = /^[a-zA-Z \-0-9']*$/;
        var r1 = /^ *$/;
        if (r.test(value) && !r1.test(value));
    }
    function is_indic(value) {
        var r;
        var r1 = /^ *$/;
        switch (lcode) {
            case 'bn': r = /^[ \-\u0980-\u09FF\u200C\u200D]*$/; break;
            case 'pa': r = /^[ \-\u0A00-\u0A7F\u200C\u200D]*$/; break;
            case 'gu': r = /^[ \-\u0A80-\u0AFF\u200C\u200D]*$/; break;
            case 'or': r = /^[ \-\u0B00-\u0B7F\u200C\u200D]*$/; break;
            case 'ta': r = /^[ \-\u0B80-\u0BFF\u200C\u200D]*$/; break;
            case 'te': r = /^[ \-\u0C00-\u0C7F\u200C\u200D]*$/; break;
            case 'kn': r = /^[ \-\u0C80-\u0CFF\u200C\u200D]*$/; break;
            case 'ml': r = /^[ \-\u0D00-\u0D7F\u200C\u200D]*$/; break;
            default: r = /^[ \-\u0900-\u097F\u1CD0-\u1CFF\uA8E0-\uA8FF\u200C\u200D]*$/; break;
        }
        return (r.test(value) && !r1.test(value));
    }
    function clear_cform() {
        $("#ceng>input").val('');
        $("#ceng>select").val('');
        $(".cin>input").val('');
        $(".cin>select").val('');
        $("#contrib_id").val(0);
        $("#contrib_subid").val(0);
        $("#contrib_count").val(1);
        $("#contrib_task").val(0);
        $(".cin").hide();
        $("#cin0").show();
    }
    function delete_entry(l, e2i, x, y, pos, ing, xs, ys, xid) {
        var flag = confirm("Do you want to delete?\n" + xs + ': ' + ys);
        var tsx = new Date().getTime();
        if (flag) {
            $.ajax({
                type: "GET",
                url: "/contribute/sktasks",
                data: { t: 50, dir: e2i, sid: x, tid: y, l: l, ts: tsx, pos: pos, ing: ing },
                dataType: "json",
                success: function (r) {
                    if (r.affected_rows == 1) {
                        $('#' + xid).css({ 'color': 'red', 'text-decoration': 'line-through' });
                    } else
                        alert(r.message + ':' + r.affected_rows + ' items deleted.');
                }
            });
        }
    }
    function edit_entry(l, dir, sid, tid, sl, tl, mf, g, left, top) {
        var w = 550;
        var h = 175;
        var xpos = 0.5 * ($(window).width() - w);
        var ypos = top - 1.33 * h;
        if (dir == 0) {
            x = sid;
            y = tid;
            xs = sl;
            ys = tl;
        }
        if (dir == 1) {
            x = tid;
            y = sid;
            xs = tl;
            ys = sl;
        }
        if (!g) g = '';
        if (!mf) mf = '';
        //$("#edit").height(h);
        $("#edit").width(w);
        $("#edit").css({
            'top': ypos, 'left': xpos,
            'position': 'absolute',
            'z-index': '100',
            'display': 'block',
            'border': 'solid 8px lightgray',
            'background': 'white',
            'padding': '10px'
        });
        $("#msg").empty();
        $("#log").empty();
        $("#edit_title").show();  // add_entry title
        $("#add_title").hide();   // edit_entry title
        $(".cin").hide();
        $("#cin0").show();
        $("#cmore0").hide();
        // Clear the contribute form
        //clear_cform();
        // Set Values
        $("#cen").val(xs);
        $("#cengrammar").val(g);
        $("#cennumber").val();
        $("#cin0>input[name=ind]").val(ys);
        $("#cin0>select[name=indgender]").val(mf);
        $("#edit").show();
        $("#cen").focus(function () {
            $("#msg").html('<p class="warn">Cannot edit this field!</p>').show();
            $(this).blur();
            $("#msg").fadeOut(1000);
        });
        $("#edit_close").click(function () {
            setMsg('', '');
            $("#cin0>input[name=ind]").removeClass('invalid');
            $("#edit").hide();
        });
        $("form#cform").submit(function () {
            setMsg('', '');
            $("#cin0>input[name='ind']").removeClass('invalid'); //{'border-color':'red', 'background-color':'#FFE377'});
            $("#contrib_eid").val(x);
            $("#contrib_iid").val(y);
            if (!is_indic($("#cin0>input[name='ind']").val())) {
                $("#cin0>input[name='ind']").addClass('invalid'); //{'border-color':'red', 'background-color':'#FFE377'});
                setMsg("warn", "This field is must be in Indian language script.");
                return false;
            }
            $("#contrib_task").val(5);
            $("#contrib_en").val($("#ceng>#cen").val());
            $("#contrib_ind").val($("#cin0>input[name='ind']").val());
            $("#contrib_indgender").val($("#cin0>select[name='indgender']").val());
            $("#contrib_indorigin").val($("#cin0>select[name='indorigin']").val());
            $("#contrib_engrammar").val($("#ceng>#cengrammar").val());
            $("#contrib_ennumber").val($("#ceng>#cennumber").val());
            var tsx = new Date().getTime();
            $("#contrib_tsx").val(tsx);
            var str = $("form#cform").serialize();
            $("#msg").empty();
            $("#log").empty();
            $.ajax({
                type: "GET",
                url: "/contribute/sktasks",
                data: str,
                dataType: "json",
                success: function (response) {
                    $("#msg").append('<p class="' + response.type + '"> Status:' + response.msg + '</p>');
                    $("#cform").children("input[type=text]").val('');
                    $("#edit_help").children().hide();
                    $("#en_help").show();
                    $("#verified").empty();
                }
            });
            return false;
        });
        return false;
    }
    //});//end of document.ready
} //end of new function define by Jitendra
function cookie(name, value) {
    if (typeof value != 'undefined') {
        if (value === null) {
            value = '';
            expires = -1;
        }
        var date = new Date();
        date.setTime(date.getTime() + (30 * 24 * 60 * 60 * 1000));
        var expires = "; expires=" + date.toGMTString();
        document.cookie = name + "=" + value + expires + "; path=/";
    } else {
        var nameEQ = name + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    }
}
function getCaret(ell) {
    if (ell.selectionStart) {
        return ell.selectionStart;
    } else if (document.selection) {
        ell.focus();

        var r = document.selection.createRange();
        if (r == null) {
            return 0;
        }

        var re = ell.createTextRange(),
        rc = re.duplicate();
        re.moveToBookmark(r.getBookmark());
        rc.setEndPoint('EndToStart', re);
       
        return rc.text.length;
    }
    return 0;
}

function setSelectionRange(input, selectionStart, selectionEnd) {
    if (input.setSelectionRange) {
        input.focus();
        input.setSelectionRange(selectionStart, selectionEnd);
    }
    else if (input.createTextRange) {
        var range = input.createTextRange();
        range.collapse(true);
        range.moveEnd('character', selectionEnd);
        range.moveStart('character', selectionStart);
        range.select();
    }
}

function setCaretToPos(input, pos) {
    setSelectionRange(input, pos, pos);
}