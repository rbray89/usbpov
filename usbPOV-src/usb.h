#include "usbdrv/usbdrv.h"

#ifndef __usb_h_included__

usbMsgLen_t usbFunctionSetup(uchar data[8]);
uchar   usbFunctionWrite(uchar *data, uchar len);
uchar   usbFunctionRead(uchar *data, uchar len);

void usbHardwareInit(void);

volatile uchar	usbDetected;

#define __usb_h_included__
#endif
