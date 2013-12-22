--Heartbeat Saver Copyright (c) <2013> <LeChosenOne> LegendCraft Team

--Permission is hereby granted, free of charge, to any person obtaining a copy
--of this software and associated documentation files (the "Software"), to deal
--in the Software without restriction, including without limitation the rights
--to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
--copies of the Software, and to permit persons to whom the Software is
--furnished to do so, subject to the following conditions:

--The above copyright notice and this permission notice shall be included in
--all copies or substantial portions of the Software.

--THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
--IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
--FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
--AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
--LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
--OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
--THE SOFTWARE.

--prettyfull welcome message
print("***Welcome to LegendCraft HeartbeatSaver***\n")
print("...:: This program is designed to send ::...\n ....:: a Heartbeat to ClassiCube.net! ::.... \n")
print(".::.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.::.\n")


--variables
rawData = {}
finalData = ""
count = 1


--send the request (called from checkData() )
function sendHeartBeat(rawData, count)
    local http = require("socket.http")
	local serverName = string.gsub(rawData[5], "(\n)", "( )")
    finalData = "http://www.classicube.net/server/heartbeat?public=" .. string.gsub(rawData[6], "(\n)", "( )") .. "&max=" .. string.gsub(rawData[4], "(\n)", "( )") .. "&users=" .. string.gsub(rawData[3], "(\n)", "( )") ..
	"&port=" .. string.gsub(rawData[2], "(\n)", "( )") .. "&version=7&salt=" .. string.gsub(rawData[0], "(\n)", "( )") .. "&name=" .. string.gsub(serverName, "( )", "%%20")
	response = http.request(finalData)
	time = os.date("*t")
	print(time.hour .. ":" .. time.min .. " - Sending Heartbeat... Count: " .. count)
    if(string.match(response, "http://www.classicube.net/server/play")) then
       print ("Heartbeat sent!\n")
        file = io.open("externalurl.txt")
	   if (file == nil) then
          print ("WARNING: externalurl.txt not found. HeartBeat Saver will now close...")
          os.sleep(5)
		  file:close()
          os.exit()
	   else
	      io.close()
		  os.remove("externalurl")
		  externalUrl = io.open("externalurl.txt", "w") --delete old file, replace with new one
          externalUrl:write(response)
          externalUrl:close()
		end
    else
        print ("Heartbeat failed: " .. response .. "\n")
	end
end

--check info from heartbeat, start the sending loop (called from main while loop)
function checkData()
    lineNum = 0
    local file = io.open("heartbeatdata.txt", "r")
	if (file == nil) then
	   io.close(file)
       print ("WARNING: Heartbeatdata.txt not found. HeartBeat Saver will now close...")
       socket.sleep(5)
       os.exit()
	end
    for item in file:lines() do
	    if(item == nil) then
		   break
		end
        rawData[lineNum] = item
        lineNum = lineNum + 1
	end
    if(lineNum ~= 7) then
        print ("WARNING: Heartbeatdata.txt has been damaged or corrupted.")
		socket.sleep(5)
        os.exit()
	end
    sendHeartBeat(rawData, count)
end

--gather data, main loop
while (true) do
    checkData()
    socket.sleep(10)
    count = count + 1
end

