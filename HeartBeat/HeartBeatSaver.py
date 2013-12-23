#Heartbeat Saver Copyright (c) <2013> <LeChosenOne> LegendCraft Team

#Permission is hereby granted, free of charge, to any person obtaining a copy
#of this software and associated documentation files (the "Software"), to deal
#in the Software without restriction, including without limitation the rights
#to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
#copies of the Software, and to permit persons to whom the Software is
#furnished to do so, subject to the following conditions:

#The above copyright notice and this permission notice shall be included in
#all copies or substantial portions of the Software.

#THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
#IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
#FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
#AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
#LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
#OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
#THE SOFTWARE.

import time;
from time import strftime;
import urllib2;
import sys;

#prettyfull welcome message
print("***Welcome to LegendCraft HeartbeatSaver***\n");
print("...:: This program is designed to send ::...\n ....:: a Heartbeat to ClassiCube.net! ::.... \n");
print(".::.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.::.\n");


#variables
rawData = []
finalData = ""
count = 1


#send the request (called from checkData() )
def sendHeartBeat(rawData, count):
    finalData = "http://www.classicube.net/heartbeat.jsp?public=" + rawData[6].replace("\n", "") + "&max=" + rawData[4].replace("\n", "") + "&users=" + rawData[3].replace("\n", "") + "&port=" + rawData[2].replace("\n", "") + "&version=7&salt=" + rawData[0].replace("\n", "") + "&name=" + (rawData[5].replace("\n", "")); 
    response = urllib2.urlopen(finalData.replace(" ","%20"));#grab the response
    responseData = response.read();
    print str(strftime("%I:%M")) + " - Sending Heartbeat... Count: " + str(count);
    if(responseData.startswith("http://www.classicube/server/play")):#check that the response does not contain errors
       print "Heartbeat sent!\n";
       try:
          with open("externalurl.txt"): pass
       except IOError:
          print "WARNING: externalurl.txt not found. HeartBeat Saver will now close..."
          time.sleep(5);
          sys.exit(0);
       externalUrlFile = open("externalurl.txt", "w");#open, wipe, and rewrite externalurl.txt
       externalUrlFile.write(responseData);
       externalUrlFile.close();
    else:
        print "Heartbeat failed: " + responseData + "\n";
    response.close();

#check info from heartbeat, start the sending loop (called from main while loop)
def checkData(): 
    lineNum = 0
    try:
        file = open("heartbeatdata.txt")
    except IOError:
        print "WARNING: Heartbeatdata.txt not found. HeartBeat Saver will now close..."
        time.sleep(5)
        sys.exit(0)
    for line in file:
        rawData.append(line)
        lineNum += 1
    if(lineNum != 7):
        print "WARNING: Heartbeatdata.txt has been damaged or corrupted."
        sys.exit(0)       
    sendHeartBeat(rawData, count)

#gather data, main loop
while(True):
    checkData();
    time.sleep(10);
    count += 1;


 