#include <avr/io.h>
#include <util/delay.h>
#include <avr/pgmspace.h>
#include <avr/interrupt.h> 
#include <avr/eeprom.h>
#include <avr/wdt.h>

#include "usbconfig.h"
#include "usbdrv/usbdrv.h"
#include "usb.h"
#include "mma7660.h"

//defines the Port that is used for the LEDs.
#define LED_PORT PORTD
#define LED_DDR  DDRD


//Space equals 32!!!
//This defines a character array based off of dot matrix font.
// the PROGMEM attribute defines that the array is to go into program memory 
// and not SRAM. it has to be accessed in the following way though:
// pgm_read_byte(&(characterArray[x][y]))
unsigned char characterArray[96][6] PROGMEM = {
{0,	0,	0,	0,	0,	0},// 
{0,	0,	125,	0,	0,	0},// !
{0,	112,	0,	112,	0,	0},// "
{20,	127,	20,	127,	20,	0},// #
{18,	42,	127,	42,	36,	0},// $
{18,	42,	127,	42,	36,	0},// %
{54,	73,	85,	34,	5,	0},// &
{0,	0,	96,	0,	0,	0},// '
{0,	28,	34,	65,	0,	0},// (
{0,	65,	34,	28,	0,	0},// )
{20,	8,	62,	8,	20,	0},// *
{8,	8,	62,	8,	8,	0},// +
{0,	0,	5,	6,	0,	0},// ,
{0,	0,	5,	6,	0,	0},// -
{0,	0,	3,	3,	0,	0},// .
{2,	4,	8,	16,	32,	0},// /
{62,	69,	73,	81,	62,	0},// 0
{0,	33,	127,	1,	0,	0},// 1
{33,	67,	69,	73,	49,	0},// 2
{34,	65,	73,	73,	54,	0},// 3
{12,	20,	36,	127,	4,	0},// 4
{114,	81,	81,	81,	78,	0},// 5
{30,	41,	73,	73,	6,	0},// 6
{96,	71,	72,	80,	96,	0},// 7
{54,	73,	73,	73,	54,	0},// 8
{48,	73,	73,	74,	60,	0},// 9
{0,	0,	54,	54,	0,	0},// :
{0,	0,	53,	54,	0,	0},// ;
{8,	20,	34,	65,	0,	0},// <
{20,	20,	20,	20,	20,	0},// =
{0,	65,	34,	20,	8,	0},// >
{32,	64,	69,	72,	48,	0},// ?
{38,	73,	79,	65,	62,	0},// @
{63,	68,	68,	68,	63,	0},// A
{127,	73,	73,	73,	54,	0},// B
{62,	65,	65,	65,	34,	0},// C
{127,	65,	65,	65,	62,	0},// D
{127,	73,	73,	73,	65,	0},// E
{127,	72,	72,	72,	64,	0},// F
{62,	65,	65,	73,	47,	0},// G
{127,	8,	8,	8,	127,	0},// H
{0,	65,	127,	65,	0,	0},// I
{2,	65,	65,	126,	64,	0},// J
{127,	8,	20,	34,	65,	0},// K
{127,	1,	1,	1,	1,	0},// L
{127,	32,	24,	32,	127,	0},// M
{127,	16,	8,	4,	127,	0},// N
{62,	65,	65,	65,	62,	0},// O
{127,	72,	72,	72,	48,	0},// P
{62,	65,	69,	66,	61,	0},// Q
{127,	72,	76,	74,	49,	0},// R
{50,	73,	73,	73,	38,	0},// S
{64,	64,	127,	64,	64,	0},// T
{126,	1,	1,	1,	126,	0},// U
{124,	2,	1,	2,	124,	0},// V
{126,	1,	14,	1,	126,	0},// W
{99,	20,	8,	20,	99,	0},// X
{112,	8,	7,	8,	112,	0},// Y
{67,	69,	73,	81,	97,	0},// Z
{0,	127,	65,	65,	0,	0},// [
{32,	16,	8,	4,	2,	0},// backslash  
{0,	65,	65,	127,	0,	0},// ]
{16,	32,	64,	32,	16,	0},// ^
{1,	1,	1,	1,	1,	0},// _
{0,	64,	32,	16,	0,	0},// `
{2,	21,	21,	21,	15,	0},// a
{127,	9,	17,	17,	14,	0},// b
{14,	17,	17,	17,	2,	0},// c
{14,	17,	17,	9,	127,	0},// d
{14,	21,	21,	21,	12,	0},// e
{8,	63,	72,	64,	32,	0},// f
{8,	21,	21,	21,	30,	0},// g
{127,	8,	16,	16,	15,	0},// h
{0,	9,	95,	1,	0,	0},// i
{0,	2,	1,	17,	94,	0},// j
{0,	127,	4,	10,	17,	0},// k
{0,	65,	127,	1,	0,	0},// l
{31,	16,	15,	16,	15,	0},// m
{31,	8,	16,	16,	15,	0},// n
{14,	17,	17,	17,	14,	0},// o
{31,	20,	20,	20,	8,	0},// p
{8,	20,	20,	12,	31,	0},// q
{31,	8,	16,	16,	8,	0},// r
{9,	21,	21,	21,	2,	0},// s
{16,	126,	17,	1,	2,	0},// t
{30,	1,	1,	2,	31,	0},// u
{28,	2,	1,	2,	28,	0},// v
{30,	1,	6,	1,	30,	0},// w
{17,	10,	4,	10,	17,	0},// x
{24,	5,	5,	5,	30,	0},// y
{17,	19,	21,	25,	17,	0},// z
{0,	8,	54,	65,	0,	0},// {
{0,	0,	127,	0,	0,	0},// |
{0,	65,	54,	8,	0,	0},// }
{4,	8,	8,	4,	8,	0},// ~
{4,	50,	2,	50,	4,	0}// SMILEY		95+32 = 0x7F
};

#define MAXPGMCHAR 96

volatile unsigned char eepromChars[8][6];
//*************  Hover Text Message  ***************
volatile unsigned char message[200];
volatile unsigned char msgLength;
volatile unsigned int  STRING_DELAY;	


volatile signed int characterPosition = 0;
volatile signed int linePosition = 0;
volatile char LEDToggle = 0;
volatile unsigned int delay = 0;
volatile char startFloat = 0;
volatile char direction = 0;

volatile unsigned int time = 0;
volatile unsigned char stringNumS = 0;
volatile unsigned char stringNumE = 0;


int main(void)
{

//USB 16 bit detection timer
TCCR1B |= ((1 << CS12));// | (1 << CS10)); // Set up timer at Fcpu/256 

	wdt_enable(WDTO_1S);
	
	usbHardwareInit();

	usbInit();
	sei();
TCNT1 = 0;
    for(;;){    // main event loop 
        wdt_reset();
        usbPoll();
//ticks = .3*12000000/256 = 23437
	if( !usbDetected && TCNT1 > 40437)
		break;
	}
//	cli();
	wdt_disable();
	EIMSK  &= ~(1<<INT0);

   TCCR1B |= ((1 << CS12));// | (1 << CS10)); // Set up timer at Fcpu/256 
   TCCR0B |= (1 << CS02) | (1 << CS00);// Set up timer at Fcpu/1024
   TCCR0A |= (1 << WGM01);//setup compare
//OCR0A - Compare Registers...
//OCR0B - Compare Registers...


//   sei(); //  Enable global interrupts 

// Set the PORTD (LEDs) as Output:
  LED_DDR= 0xFF;
    
  // Turn off LEDs
  LED_PORT = 0xF0;

	//Initialize the accelarometer.
MMA7660_Init();

//EEPROM Memory Format... 

//First Byte (adress 0) contains the message length.
//Second Byte to 200 contain the message.
//Remainder: to be determined.

//Fetch Message Length From EEPROM
msgLength = eeprom_read_byte((uint8_t*)0); 

//Fetch Message from EEPROM
eeprom_read_block((void*)&message, (const void*)1, 200); 

//Fetch delay time from EEPROM
STRING_DELAY = eeprom_read_word ((uint16_t*)201);
//STRING_DELAY = 2000;

//Add extra chars stored in EEPROM
eeprom_read_block((void*)&eepromChars, (const void*)203, 48); 



//Fix the Setup messsage end part...
	while(message[stringNumE] != '\n'){
		stringNumE++;
	}
	stringNumE--;


//Each timer tick = .000032s
//Each char therfore consists of approx. 318 ticks. = 6x1.7ms= 


char lock = 0;
char timeLock = 1;
char first = 1;
char left = 0;
//char right = 0;
char floatReady = 0;


unsigned int msgStart = 0;
unsigned int cycleCount = 0;
unsigned int start = 0;
unsigned int charDelay = 0;


TCNT1 = 0;
TCNT0 = 0;

  while(1){

	//get accelerometer values
	signed char accel = MMA7660_GetVal(YOUT);

	//find high accel
	if(accel <= -32 && !lock){

	lock = 1;
	timeLock = 0;
	//count a oscillation cycle
	cycleCount = TCNT1;
	TCNT1 = 0;
	//add offset to message as accel values are skewed slightly towards one direction of movement
	msgStart = cycleCount*3/11;


//make sure at least one cycle is recorded before message appears
	if(first == 2)
		first = 0;
	if(first == 1)
		first = 2;

	floatReady = 1;	
	left = 1;
	}
/*	else if(!first && accel >= 31 && !lock){

	lock = 1;
	timeLock = 0;
	
	msgStart = cycleCount/2 + cycleCount/6;

//	floatReady = 1;
//	right = 1;
	}
*/
	else if(first !=1 && lock && accel < 31 && accel > -32){
	lock = 0;
	
	}
	if(first < 1 && TCNT1 > msgStart && floatReady){
		floatReady = 0;
		if(left){
			left = 0;
			direction = 1;
		}
/*		else if(right){
			right = 0;
			direction = 0;
		}
*/

			startFloat = 1;
			//Calculate start time to float message...
			//1/4 of a cycle is half of a single wave, minus half the time to float the message... 
			//divied charDelay by an additional 4 to compensate for Timer0-1 prescale difference 
			charDelay = (cycleCount/16)/(stringNumE-stringNumS+1);	
			delay = charDelay/6;			
			start = charDelay/2;
			//reset message timer
			TCNT0 = 0;
			//Enable Output Compare A Interrupts on Timer0
			OCR0A = start;
			//enable interrupts on compare for Timer0
			TIMSK0 |= (1 << OCIE0A);

	}
	


  }

  return 0;
}

//Timer 0 interrupt service routine. 

//This is what grabs the value stored in SRAM for the image, and puts it up on to the LEDs, turns them off, then
//moves to the next row.
ISR(TIMER0_COMPA_vect)
{


time += OCR0A;

//beginning of message
if(startFloat == 1){
	startFloat = 2;
//reset position of chars
	if(direction == 1){
		characterPosition = stringNumS;
		linePosition = 0;
	}
/*	else{
		characterPosition = stringNumE;
		linePosition = 5;
	}
*/
//end of message
}else if(startFloat == 0){
//disable timer intterupt at end
	TIMSK0 &= ~(1 << OCIE0A);
//if next message break...
	if (time >= STRING_DELAY){
		time = 0;
//find next message basically, and set begining and end of string markers
		while(message[stringNumS] != '\n'){
			stringNumS++;
		}
		stringNumS++;
		if(stringNumS >= msgLength){
			stringNumS = 0;
			stringNumE = 0;
		}else{
			stringNumE += 2;
		}
			while(message[stringNumE] != '\n'){
				stringNumE++;
			}
			stringNumE--;
	}
}
//toggle the leds
if(!LEDToggle){
	unsigned char leds;
	LEDToggle = 1;
	unsigned char character = message[characterPosition]-' ';
	if(character >= MAXPGMCHAR)
	leds = eepromChars[character-MAXPGMCHAR][linePosition];
	else
	leds = (unsigned char)pgm_read_byte(&(characterArray[character][linePosition]));
//due to hardware, the top four leds need to be configured to be active LOW...
	leds = ((~leds)&0xF0)|(leds&0x0F);
	LED_PORT =  leds;
//reset timer
	OCR0A = delay/3;
	if(direction == 1){

//increment line position, if at end, goto next char, if past, reset float string to be done.
		linePosition++;
		if(linePosition == 6){
			linePosition = 0;
			characterPosition++;
		}
		if(characterPosition > stringNumE){
			startFloat = 0;
		}

	}
/*	else{
		
		linePosition--;
		if(linePosition == -1){
			linePosition = 5;
			characterPosition--;
		}
		if(characterPosition < stringNumS)
			startFloat = 0;

	}
*/
}
//turn leds off and reset timer.
else{
	LEDToggle = 0;
	LED_PORT =  0xF0;
	OCR0A = delay*2/3;

}


 
}
