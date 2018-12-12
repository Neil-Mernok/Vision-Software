using FlagsEnumTypeConverter;
using System;
using System.ComponentModel;

namespace Vision
{
    //[TypeConverter(typeof(FlagsEnumConverter))]
    [Flags]
    public enum TagActivities// : uint
    {
        [Browsable(false)] none = 0,
        [Browsable(false)] tag_enable = 1 << 0,                // turn the device on. otherwise it sleeps.                                  
        [Browsable(false)] broadcast_ID = 1 << 1,              // will send RF packet every <period> milliseconds                           
        [Browsable(false)] heartbeat = 1 << 2,                 // will send out a periodic heartbeat                                        
        [Browsable(false)] LF_TX = 1 << 3,                     // will transmit LF TX message at the given interval                         
        [Browsable(false)] LF_response = 1 << 4,               // will send RF packets when an LF packet is received                        
        [Browsable(false)] receive_RF = 1 << 5,                // will receive RF packets from other sources                                
        [Browsable(false)] Always_on = 1 << 6,                 // will never enter low power state (irrespective of being battery powered)  
        [Browsable(false)] accept_data = 1 << 7,               // will forward data packets to master.                                      
        [Browsable(false)] output_critical = 1 << 8,           // this is a test mode setting which will output critical distances to a GPIO
        [Browsable(false)] CAN_terminated = 1 << 9,
        [Browsable(false)] forward_RF = 1 << 10,               // this device will forward nay RF packet to master. for testing             
        [Browsable(false)] CAN_sync = 1 << 11,                 // this device will send a sync message over the can when done sending LF.   
        [Browsable(false)] get_range_all = 1 << 12,            // will request a range for all devices ID's                                 
        [Browsable(false)] get_range_select = 1 << 13,         // will request range from certain devices based on type.                    
        [Browsable(false)] forward_dists = 1 << 14,            // will pass applicable range results directly to master.                    
        [Browsable(false)] use_shortened_fw = 1 << 15,         // use a shortened auto-forward message for the vision messages.              
        [Browsable(false)] send_name = 1 << 16,                // this tag will send its name string with RF messages     
        [Browsable(false)] legacy_PDS = 1 << 17,               // this tag will send the shortened, old PDS message when it sends a RF packet.
        [Browsable(false)] forward_own_lf = 1 << 18,           // this device will send messages to its master reporting its own LF levels. 
        [Browsable(false)] disable_exclusion = 1 << 19,        // Device will ignore exclusion LF messages (useful for vehicle tags)
        [Browsable(false)] disable_LF_CRC = 1 << 20,           // Device will send LF response even if LF CRC failed.
        [Browsable(false)] GPS_Capable = 1 << 21,               // Device will send LF response even if LF CRC failed.
        [Browsable(false)] Heartbeat_monitor = 1 << 22,          // this will enable the tag to do heartbeat monitoring. Currently used for Trafic light applications.
        [Browsable(false)] Broadcast_time = 1 << 23,

        //[Browsable(false)]
        Pulse100 = tag_enable | broadcast_ID | LF_response | output_critical | send_name,
        //[Browsable(false)]
        Pulse300 = tag_enable | broadcast_ID | heartbeat | LF_response | output_critical | use_shortened_fw | disable_exclusion,
        //[Browsable(false)]
        Pulse400 = tag_enable | heartbeat | receive_RF | Always_on | forward_RF | forward_dists,
        //[Browsable(false)]
        Pulse500 = tag_enable | heartbeat | Always_on | LF_TX | LF_response | CAN_sync | use_shortened_fw | disable_exclusion,
        //[Browsable(false)]
        Ranger = tag_enable | broadcast_ID | heartbeat | Always_on | accept_data| receive_RF | get_range_all| forward_dists,
        //[Browsable(false)]
        GPS_Module = tag_enable | heartbeat | Always_on | accept_data | receive_RF | forward_dists | forward_RF,

    }
}
