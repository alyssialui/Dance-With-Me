//chuck program to play notes acc. to events received from the Wekinator

// create the OSC receiver
OscRecv recv;
// use port 12000
12000 => recv.port;
// start listening (launch thread)
recv.listen();

// create an address in the receiver, store in new variable
recv.event( "/wek/outputs, f f f f f" ) @=> OscEvent oe;


// play notes using clarinet
Clarinet clair => JCRev r => dac;
.75 => r.gain;
.1 => r.mix;

// Do Re Mi Fa So La Ti Do 
[ 61, 63, 65, 66, 68, 70, 72, 73 ] @=> int notes[];


fun void setParams(float params[])
{
	//Uncomment to test if notes array is correct
    //for( int i; i < notes.cap(); i++ )
    //{
      //  play( 12 + notes[i], Math.random2f( .6, .9 ) );
       // 300::ms => now;
    //}
	
	// clear
    clair.clear( 1.0 );

    // set
    Math.random2f( 0, 1 ) => clair.reed; //reed stiffness
    Math.random2f( 0, 1 ) => clair.noiseGain; // noise gain
    Math.random2f( 0, 12 ) => clair.vibratoFreq; // vibrato freq
    Math.random2f( 0, 1 ) => clair.vibratoGain; //vibrato gain
    Math.random2f( 0, 1 ) => clair.pressure; //breath pressure
    
    0 => int myNote;
    
    if(params[0] == 1)
        notes[0] => myNote;
    else if (params[0] == 2)
        notes[1] => myNote;
    else if (params[0] == 3)
        notes[2] => myNote;
    else if (params[0] == 4)
        notes[3] => myNote;
    else if (params[0] == 5)
        notes[4] => myNote;
    else if (params[0] == 6)
        notes[5] => myNote;
    else if (params[0] == 7)
        notes[6] => myNote;
    else if (params[0] == 8)
        notes[7] => myNote;
    

    play(12 + myNote, params[1] , params[2]);
    //2::second => now;
    
}


// basic play function (add more arguments as needed)
fun void play( float note, float velocity, float volume  )
{
    // start the note
    Std.mtof( note ) => clair.freq;
    velocity => clair.noteOn;
    volume => clair.vibratoGain;
}



fun void waitForEvent() {
    // infinite event loop
    while ( true )
    {
        // wait for event to arrive
        oe => now;
        
        // grab the next message from the queue. 
        while ( oe.nextMsg() != 0 )
        { 
            float p[5];
            oe.getFloat() => p[0]; //MIDI note : 1 - 8
            oe.getFloat() => p[1]; //velocity : 0 - 2
            oe.getFloat() => p[2]; //volume : 0.0 - 1.0
            oe.getFloat() => p[3];
            oe.getFloat() => p[4];
            setParams(p);
        }
    }   
    
}

[ 2.0, .9, 0.9, 0.0, 0.0 ] @=> float p[];
setParams(p);
spork ~waitForEvent();
1::hour => now;
