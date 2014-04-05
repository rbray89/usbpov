#include <avr/io.h>
#include "mma7660.h"

void Write_MMA7660(unsigned char reg_addr,unsigned char data)
{
   // Start the I2C Write Transmission
   i2c_start(MMA7660_ADDR,TW_WRITE);

   // Sending the Register Address
   i2c_write(reg_addr);

   // Write data to MCP23008 Register
   i2c_write(data);

   // Stop I2C Transmission
   i2c_stop();
}

unsigned char Read_MMA7660(unsigned char reg_addr)
{
   char data;

   // Start the I2C Write Transmission
   i2c_start(MMA7660_ADDR,TW_WRITE);

   // Read data from MCP23008 Register Address
   i2c_write(reg_addr);

   // Stop I2C Transmission
  //i2c_stop();

   // Re-Start the I2C Read Transmission
   i2c_start(MMA7660_ADDR,TW_READ);

   i2c_read(&data,NACK);

   // Stop I2C Transmission
   i2c_stop();

   return data;
}

void MMA7660_Init(void){


i2c_init();

Write_MMA7660(MODE,0x00);//Standby Mode -- allows config to be written
Write_MMA7660(SPCNT,0x00);//0 Sleep count
Write_MMA7660(INTSU,INTSU_GINT|INTSU_SHINTY);//No Interrupts - including GINT
Write_MMA7660(PDET,PDET_XDA|PDET_YDA|PDET_ZDA);//Tap detection is disabled, no threshold
Write_MMA7660(SR,SR_AMSR_AMPD|0x60);//32 samples/second, 4 matches for tilt
Write_MMA7660(PD,0x00);//No Tap Filtering
Write_MMA7660(MODE,0x01);//Active Mode -- Ready

}

char MMA7660_GetVal(char axis){

char reg;

reg = Read_MMA7660(axis);

while(reg&ALERT_MASK)
reg = Read_MMA7660(axis);

reg = reg&OUT_VALUE_MASK;

if(OUT_VALUE_SIGN&reg)
reg = reg|0xC0;

return reg;

}


char MMA7660_GetTilt(void){

char reg;

reg = Read_MMA7660(TILT);


while(reg&ALERT_MASK)
reg = Read_MMA7660(TILT);


return reg;

}
