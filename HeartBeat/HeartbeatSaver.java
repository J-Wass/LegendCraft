//Heartbeat Saver Copyright (c) <2013> <LeChosenOne> LegendCraft Team

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

//just randomly import all the stuff I think I may need
import java.lang.*;
import java.util.Date;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.Scanner;
import java.net.*;
import java.io.*;
import java.util.Date;
import java.text.DateFormat;
import java.text.SimpleDateFormat;

public class HeartbeatSaver
{

//variables
static String finalData = "";
static int count = 0;
static String pub, max, users, port, salt, name, placeholder;
static String response = " ";
static DateFormat dFormat = new SimpleDateFormat("HH:mm");
static Date date = new Date();
static URL url;
static Scanner scanner;

//send the request (called from checkData() )
public static void sendHeartBeat()
{
    finalData = "http://www.classicube.net/server/heartbeat?public=" + pub + "&max=" + max + "&users=" + users + "&port=" + port + "&version=7&salt=" + salt + "&name=" + name; 
	try
	{
	    url = new URL(finalData);
		scanner = new Scanner(url.openStream());//open webpage with data, scan the page
	}
	catch(MalformedURLException mal)
	{
	    System.out.println("WARNING: URL was not created correctly, system will now close.");
		try
		{
            Thread.sleep(5000);
		}
		catch(InterruptedException ex)
	    {
		    System.exit(0); 
		}
		System.exit(0);
	}
	catch(IOException io)
	{
	    System.out.println("WARNING: URL was not created correctly, system will now close.");
		try
		{
            Thread.sleep(5000);
		}
		catch(InterruptedException ex)
	    {
		    System.exit(0); 
		}
		System.exit(0);
	}	
	String response = scanner.nextLine();

    System.out.println("\n" + dFormat.format(date) + " - Sending Heartbeat... Count: " + count);
    if(response.startsWith("http://www.classicube.net/server/play"))//check that the response does not contain errors
    {
	   System.out.println("Heartbeat sent!");
       try
	   {
	       File exUrl = new File("externalurl.txt"); //open, delete, and create new externalurl.txt
		   exUrl.delete();
		   PrintWriter wr = new PrintWriter("externalurl.txt", "UTF-8");
		   wr.println(response);
		   wr.close();	   
	   }
       catch(Exception e)
	   {
           System.out.println("WARNING: externalurl.txt not found. HeartBeat Saver will now close...");
           try
		   {
               Thread.sleep(5000);
		   }
		   catch(InterruptedException ex)
		   {
		       System.exit(0); 
		   }
           System.exit(0);
	   }
	}   
    else
	{
        System.out.println("Heartbeat failed: " + response);
	}
}

//check info from heartbeat, start the sending loop (called from main while loop)
public static void checkData()
{
    int lineNum = 0;
    try
	{
	    File hb = new File("heartbeatdata.txt");
		Scanner s = new Scanner(hb);
	    salt = s.nextLine();
	    placeholder = s.nextLine();
	    port = s.nextLine();
	    users = s.nextLine();
	    max = s.nextLine();
	    name = s.nextLine().replace(" ", "%20");//url can't have a space, so use %20
	    pub = s.nextLine();
    }
    catch(Exception e)
	{
        System.out.println("WARNING: Heartbeatdata.txt not found. HeartBeat Saver will now close...");
		try
		{
           Thread.sleep(5000);
		}
		catch(InterruptedException ex)
		{
		    System.exit(0); 
		}
        System.exit(0);
	}

    if(pub == null || name == null || max == null || users == null || port == null || salt == null)
	{
        System.out.println("WARNING: Heartbeatdata.txt has been damaged or corrupted.");	
		try
		{
           Thread.sleep(5000);
		}
		catch(InterruptedException e)
		{
		    System.exit(0); 
		}
        System.exit(0);      
    }		
    sendHeartBeat();
}

//gather data, main loop
public static void mainLoop()
{
    while(true)
    {
        count++;
		checkData();
	    try
	    {
           Thread.sleep(10000); //loop every 10s
	    }
	    catch(InterruptedException e)
	    {
	       //Should work :P, just normal for java to have catch-tries around Thread.sleep
	    }
    }
}

//prettyfull welcome message
public static void main(String[] args)
{
   System.out.println("***Welcome to LegendCraft HeartbeatSaver***\n");
   System.out.println("...:: This program is designed to send ::...\n ....:: a Heartbeat to ClassiCube.net! ::.... \n");
   System.out.println(".::.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.::.\n");
   mainLoop();
}
}

//half of my code is try-catch, i will murder Oracle
 