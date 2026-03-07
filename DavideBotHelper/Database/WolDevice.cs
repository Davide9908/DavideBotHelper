using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DavideBotHelper.Database;

[Table("wol_device")]
public class WolDevice
{
    [Column("wol_device_id")]
    public int WolDeviceId { get; set; }
    [Column("device_mac_address"), MaxLength(17)]
    public string DeviceMacAddress { get; set; } = null!;
    [Column("description"), MaxLength(100)]
    public string? Description { get; set; }
    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    public override string ToString()
    {
        return string.Join('-', WolDeviceId, Description, DeviceMacAddress);
    }
}