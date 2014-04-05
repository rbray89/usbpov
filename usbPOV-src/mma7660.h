#include "i2cmaster.h"

//I2C address locations
#define MMA7660_ADDR  0x98  	//Translated (1 bit left) MMA7660 Device Address


//************  MMA7660 Register addresses  *************


//^^^^^^^^^^^^  READ ONLY REGISTERS  ^^^^^^^^^^^^^^^^^^^^
// These 4 registers have been shifted one bit to accomidate the extra read signal bit.

//Signed byte 6-bit 2’s complement data with allowable range of +31 to -32.
//_OUT[5] is 0 if the g direction is positive, 1 if the g direction is negative.
#define XOUT  0x00           	// MMA7660 XOUT Address
#define YOUT  0x01           	// MMA7660 YOUT Address
#define ZOUT  0x02           	// MMA7660 ZOUT Address
#define TILT  0x03           	// MMA7660 TILT Address



//^^^^^^^^^^^^  Configuration Registers  ^^^^^^^^^^^^^^^^
#define SRST  0x04           	// MMA7660 Sampling rate status Address 

//Sets the 8-bit maximum count value for the 8-bit internal sleep counter in Auto-Sleep. When the 8-bit internal sleep counter
//reaches the value set by SC[7:0], MMA7660FC will exit Auto-Sleep and switch to the samples per second specified in AWSR[1:0]
//of the SR (0x08) register.
#define SPCNT  0x05           	// MMA7660 Sleep count Address 

#define INTSU  0x06           	// MMA7660 Interrupt setup Address 

//Writing to the Mode register resets sleep timing, and clears the XOUT, YOUT, ZOUT, TILT registers.Reading to
//the Mode register resets sleep timing.
//NOTE: The device must be placed in Standby Mode to change the value of the registers.
#define MODE  0x07           	// MMA7660 Mode Address 

#define SR  0x08           	// MMA7660 Auto-Wake/Sleep and 
				//Portrait/Landscape samples 
				//per seconds and Debounce Filter Address 

// If XDA = YDA = ZDA = 0, samples per second is 120 samples/second,
// and Auto-Wake/Sleep feature is enabled,
// the tap interrupt will reset the sleep counter.
#define PDET  0x09           // MMA7660 Tap detection Address 

// The tap detection debounce filtering requires 2-256 adjacent
// tap detection tests to be the same to trigger a tap event 
// and set the Tap bit in the TILT register, and optionally 
// set an interrupt if PDINT is set in the INTSU register. 
#define PD  0x0A           // MMA7660 Tap debounce Count Address 


//***********************************************************************
//This section defines the various masks and filters for certain bits for
//the various registers indexed above.
//***********************************************************************
#define OUT_VALUE_MASK 0x3F 	//Mask for accelarometer values; USE: XOUT&ACCEL_VALUE_MASK to read
#define OUT_VALUE_SIGN 0x20 	//Mask for accelarometer values; USE: XOUT&ACCEL_VALUE_MASK to read
#define ALERT_MASK 0x40		//Mask for the Alert bit in XOUT,YOUT,ZOUT, and TILT
//If the Alert bit is set, the register was read at the same time as the device was attempting to update the contents. The register
//must be read again.

//************  TILT Masks  *************

#define TILT_BAFRO	0x03	//00:Unknown condition of front or back
				//01: Front: Equipment is lying on its front
				//10: Back: Equipment is lying on its back

#define TILT_POLA	0x1C	//000: Unknown condition of up or down or left or right
				//001: Left: Equipment is in landscape mode to the left
				//010: Right: Equipment is in landscape mode to the right
				//101: Down: Equipment standing vertically in inverted
				//orientation
				//110: Up: Equipment standing vertically in normal orientation

#define TILT_TAP	0x20	//1: Equipment has detected a tap
				//0: Equipment has not detected a tap

#define TILT_SHAKE	0x80	//0: Equipment is not experiencing shake in one or more of the
				//axes enabled by SHINTX, SHINTY, and SHINTZ
				//1: Equipment is experiencing shake in one or more of the
				//axes enabled by SHINTX, SHINTY, and SHINTZ

//************  SRST Masks  *************

#define SRST_AMSRS	0x01	//0: Samples per second specified in AMSR[2:0] is not active
				//1: Samples per second specified in AMSR[2:0] is active

#define SRST_AWSRS	0x02	//0: Samples per second specified in AWSR[1:0] is not active
				//1: Samples per second specified in AWSR[1:0] is active


//************  INTSU Masks  *************

#define INTSU_FBINT	0x01	//0: Front/Back position change does not cause an interrupt
				//1: Front/Back position change causes an interrupt

#define INTSU_PLINT	0x02	//0: Up/Down/Right/Left position change does not cause an
				//interrupt
				//1: Up/Down/Right/Left position change causes an interrupt

#define INTSU_PDINT	0x04	//0: Successful tap detection does not cause an interrupt
				//1: Successful tap detection causes an interrupt

#define INTSU_ASINT	0x08	//0: Exiting Auto-Sleep does not cause an interrupt
				//1: Exiting Auto-Sleep causes an interrupt

#define INTSU_GINT	0x10	//0: There is not an automatic interrupt after every measurement
				//1: There is an automatic interrupt after every measurement,
				//when g-cell readings are updated in XOUT, YOUT, ZOUT
				//registers, regardless of whether the readings have changed
				//or not. This interrupt does not affect the Auto-Sleep or Auto-
				//Wake functions.

#define INTSU_SHINTX	0x80	//0: Shake on the X-axis does not cause an interrupt or set the
				//Shake bit in the TILT register
				//1: Shake detected on the X-axis causes an interrupt, and sets
				//the Shake bit in the TILT register

#define INTSU_SHINTY	0x40	//0: Shake on the Y-axis does not cause an interrupt or set the
				//Shake bit in the TILT register
				//1: Shake detected on the Y-axis causes an interrupt, and sets
				//the Shake bit in the TILT register

#define INTSU_SHINTZ	0x20	//0: Shake on the Z-axis does not cause an interrupt or set the
				//Shake bit in the TILT register
				//1: Shake detected on the Z-axis causes an interrupt, and sets
				//the Shake bit in the TILT register.

//************  MODE Masks  *************
//Writing to the Mode register resets sleep timing, and clears the XOUT, YOUT, ZOUT, TILT registers.Reading to
//the Mode register resets sleep timing.
//NOTE: The device must be placed in Standby Mode to change the value of the registers.

#define MODE_MODE	0x01	//0: Standby mode or Test Mode depending on state of TON
				//1: Active mode
				//Existing state of TON bit must be 0, to write MODE = 1. Test
				//Mode must not be enabled.
				//MMA7660FC always enters Active Mode using the samples
				//per second specified in AMSR[2:0] of the SR (0x08) register.
				//When MMA7660FC enters Active Mode with
				//[ASE:AWE] = 11, MMA7660FC operates Auto-Sleep
				//functionality first.

#define MODE_TON	0x04	//0: Standby Mode or Active Mode depending on state of MODE
				//1: Test Mode
				//Existing state of MODE bit must be 0, to write TON = 1.
				//Device must be in Standby Mode.
				//In Test Mode (TON = 1), the data in the XOUT, YOUT and
				//ZOUT registers is not updated by measurement, but is
				//instead updated by the user through the I2C interface for test
				//purposes. Changes to the XOUT, YOUT and ZOUT register
				//data is processed by MMA7660FC to change orientation
				//status and generate interrupts just like Active Mode.
				//Debounce filtering and shake detection are disabled in Test
				//Mode

#define MODE_AWE	0x08	//0: Auto-Wake is disabled
				//1: Auto-Wake is enabled.
				//When Auto-Wake functionality is operating, the AWSRS bit is
				//the SRST register is set and the device uses the samples per
				//second specified in AWSR[1:0] of the SR (0x08) register.
				//When MMA7660FC automatically exits Auto-Wake by a
				//selected interrupt, the device will then switch to the samples
				//per second specified in AMSR[2:0] of the SR (0x08) register.
				//If ASE = 1, then Auto-Sleep functionality is now enabled

#define MODE_ASE	0x10	//0: Auto-Sleep is disabled
				//1: Auto-Sleep is enabled
				//When Auto-Sleep functionality is operating, the AMSRS bit is
				//the SRST register is set and the device uses the samples per
				//second specified in AMSR[2:0] of the SR (0x08) register.
				//When MMA7660FC automatically exits Auto-Sleep because
				//the Sleep Counter times out, the device will then switch to the
				//samples per second specified in AWSR[1:0] of the SR
				//register. If AWE = 1, then Auto-Wake functionality is now
				//enabled

#define MODE_SCPS	0x20	//0: The prescaler is divide-by-1. The 8-bit internal Sleep
				//Counter input clock is the samples per second set by
				//AMSR[2:0], so the clock range is 120 Hz to 1 Hz depending
				//on AMSR[2:0] setting. Sleep Counter timeout range is
				//256 times the prescaled clock (see Table 12).
				//1: Prescaler is divide-by-16. The 8-bit Sleep Counter input
				//clock is the samples per second set by AMSR[2:0] divided by
				//16, so the clock range is 4 Hz to 0.0625 Hz depending on
				//AMSR[2:0] setting. Sleep Counter timeout range is 256 times
				//the prescaled clock (see Table 12).

#define MODE_IPP	0x40	//0: Interrupt output ^INT is open-drain.
				//1: Interrupt output ^INT is push-pull

#define MODE_IAH	0x80	//0: Interrupt output ^INT is active low
				//1: Interrupt output ^INT is active 


//NOTE: If interrupts are enabled, interrupts will behave normally in all conditions stated in the following:

//Condition		Auto-Wake (Sleep Mode) Auto-Sleep (Run Mode)
//AWE = 0, ASE = 0					X
//AWE = 1, ASE = 0		X
//AWE = 0, ASE = 1		X 			X
//AWE = 1, ASE = 1 		X 			X


//AMSR			SCPS = 0  			SCPS = 1
//	Minimum Range (20) Maximum Range (28) Minimum Range (20) Maximum Range (28)
//1 SPS		1s 		256s		16s		4096s
//2 SPS		0.5s 		128s 		8s 		2048s
//4 SPS		0.25s 		34s 		4s 		1024s	
//8 SPS		0.125s 		32s 		2s 		512s
//16 SPS	0.625s 		16s 		1s 		256s
//32 SPS	0.03125s 	8s 		0.5s 		128s
//64 SPS	0.0156s 	4s 		0.25s 		64s
//120 SPS 	0.00836s 	2.14s 		0.133s 		34.24s


//************  SR Masks  *************

#define SR_AMSR		0x07	
#define SR_AMSR_AMPD	0x00	//000 - Tap Detection Mode and 120 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM64	0x01	//001 - 64 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM32	0x02	//010 - 32 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM16	0x03	//011 - 16 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM8	0x04	//100 - 8 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM4	0x05	//101 - 4 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM2	0x06	//110 - 2 Samples/Second Active and Auto-Sleep Mode
#define SR_AMSR_AM1	0x07	//111 - 1 Samples/Second Active and Auto-Sleep Mode

#define SR_AWSR		0x18	
#define SR_AWSR_AW32	0x00	//00 - 32 Samples/Second Auto-Wake Mode
#define SR_AWSR_AW16	0x08	//01 - 16 Samples/Second Auto-Wake Mode
#define SR_AWSR_AW8	0x10	//10 - 8 Samples/Second Auto-Wake Mode
#define SR_AWSR_AW1	0x18	//11 - 1 Samples/Second Auto-Wake Mode

#define SR_FILT		0xE0	// Debounce Filtering for tilt : 0-8 measurement samples
				// must match before device updates status for portrait
				// or landscape

//************  PDET Masks  *************
#define PDET_PDTH	0x1F	// 00000 - Tap detection is +/- 1 count
				// 00001 - Tap detection is +/- 1 counts
				// 00010 - Tap detection is +/- 2 counts...
				// 11111 - Tap detection is +/- 31 counts

#define PDET_XDA	0x20	//1: X-axis is disabled for tap detection
				//0: X-axis is enabled for tap detection

#define PDET_YDA	0x40	//1: Y-axis is disabled for tap detection
				//0: Y-axis is enabled for tap detection

#define PDET_ZDA	0x80	//1: Z-axis is disabled for tap detection
				//0: Z-axis is enabled for tap detection
//function declarations
void Write_MMA7660(unsigned char reg_addr,unsigned char data);

unsigned char Read_MMA7660(unsigned char reg_addr);

void MMA7660_Init(void);

char MMA7660_GetVal(char axis);
char MMA7660_GetTilt(void);
