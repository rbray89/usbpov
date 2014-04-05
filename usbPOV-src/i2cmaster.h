#include <compat/twi.h>
#include <avr/io.h>

#define MAX_TRIES 90

#define I2C_START 0
#define I2C_DATA 1
#define I2C_DATA_ACK 2
#define I2C_STOP 3
#define ACK 1
#define NACK 0

#define DATASIZE 32

unsigned char i2c_transmit(unsigned char type);

char i2c_start(unsigned int dev_addr, unsigned char rw_type);

void i2c_stop(void);

char i2c_write(char data);

char i2c_read(char *data,char ack_type);

void i2c_init(void);
