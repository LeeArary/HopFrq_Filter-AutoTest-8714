/**
  ******************************************************************************
  * @file    usb_prop.h
  * @author  MCD Application Team
  * @version V4.0.0
  * @date    21-January-2013
  * @brief   All processing related to Relay Mouse demo
  ******************************************************************************
  * @attention
  *
  * <h2><center>&copy; COPYRIGHT 2013 STMicroelectronics</center></h2>
  *
  * Licensed under MCD-ST Liberty SW License Agreement V2, (the "License");
  * You may not use this file except in compliance with the License.
  * You may obtain a copy of the License at:
  *
  *        http://www.st.com/software_license_agreement_liberty_v2
  *
  * Unless required by applicable law or agreed to in writing, software 
  * distributed under the License is distributed on an "AS IS" BASIS, 
  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  * See the License for the specific language governing permissions and
  * limitations under the License.
  *
  ******************************************************************************
  */


/* Define to prevent recursive inclusion -------------------------------------*/
#ifndef __USB_PROP_H
#define __USB_PROP_H

/* Includes ------------------------------------------------------------------*/
/* Exported types ------------------------------------------------------------*/
typedef enum _HID_REQUESTS
{
  GET_REPORT = 1,
  GET_IDLE,
  GET_PROTOCOL,

  SET_REPORT = 9,
  SET_IDLE,
  SET_PROTOCOL
} HID_REQUESTS;

/* Exported constants --------------------------------------------------------*/
/* Exported macro ------------------------------------------------------------*/
/* Exported functions ------------------------------------------------------- */
void Relay_init(void);
void Relay_Reset(void);
void Relay_SetConfiguration(void);
void Relay_SetDeviceAddress (void);
void Relay_Status_In (void);
void Relay_Status_Out (void);
RESULT Relay_Data_Setup(uint8_t);
RESULT Relay_NoData_Setup(uint8_t);
RESULT Relay_Get_Interface_Setting(uint8_t Interface, uint8_t AlternateSetting);
uint8_t *Relay_GetDeviceDescriptor(uint16_t );
uint8_t *Relay_GetConfigDescriptor(uint16_t);
uint8_t *Relay_GetStringDescriptor(uint16_t);
RESULT Relay_SetProtocol(void);
uint8_t *Relay_GetProtocolValue(uint16_t Length);
RESULT Relay_SetProtocol(void);
uint8_t *Relay_GetReportDescriptor(uint16_t Length);
uint8_t *Relay_GetHIDDescriptor(uint16_t Length);

/* Exported define -----------------------------------------------------------*/
#define Relay_GetConfiguration          NOP_Process
//#define Relay_SetConfiguration          NOP_Process
#define Relay_GetInterface              NOP_Process
#define Relay_SetInterface              NOP_Process
#define Relay_GetStatus                 NOP_Process
#define Relay_ClearFeature              NOP_Process
#define Relay_SetEndPointFeature        NOP_Process
#define Relay_SetDeviceFeature          NOP_Process
//#define Relay_SetDeviceAddress          NOP_Process

#define REPORT_DESCRIPTOR                  0x22

#endif /* __USB_PROP_H */

/************************ (C) COPYRIGHT STMicroelectronics *****END OF FILE****/
