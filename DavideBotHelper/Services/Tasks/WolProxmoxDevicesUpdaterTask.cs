using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using DavideBotHelper.Database;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using static Corsinvest.ProxmoxVE.Api.PveClient.PveNodes;

namespace DavideBotHelper.Services.Tasks;

public class WolProxmoxDevicesUpdaterTask : TransactionalTask
{
    private readonly ILogger<WolProxmoxDevicesUpdaterTask> _log;
    private readonly DavideBotDbContext _dbContext;
    private readonly PveClient _pveClient;
    
    private const string ProxmoxNode = "davide-homelab";
    
    public WolProxmoxDevicesUpdaterTask(ILogger<WolProxmoxDevicesUpdaterTask> log, DavideBotDbContext context, IConfiguration configuration) : base(log, context)
    {
        _log = log;
        _dbContext = context;
        string proxmoxHost = configuration["ProxmoxHost"] ?? throw new InvalidOperationException("ProxmoxHost configuration missing");
        string tokenApi = configuration["ProxmoxApiCompleteToken"] ?? throw new InvalidOperationException("ProxmoxApiCompleteToken configuration missing");
        _pveClient = new PveClient(proxmoxHost);
        _pveClient.ApiToken = tokenApi;
    }

    protected override async Task Run()
    {
        PveNodeItem node = _pveClient.Nodes[ProxmoxNode] ?? throw new InvalidOperationException($"ProxmoxNode {ProxmoxNode} seems to not exist");
        var vms = (await node.Qemu.GetAsync()).ToList();
        var containers =  (await node.Lxc.GetAsync()).ToList();
        var availableVms = new List<(long vmId, VmType vmType)>(vms.Count + containers.Count);
        availableVms.AddRange(vms.Select(x => (x.VmId, VmType.VirtualMachine)));
        availableVms.AddRange(containers.Select(x => (x.VmId, VmType.Container)));

        var wolDevices = await _dbContext.WolDevices.ToListAsync();
        List<WolDevice> newDevices = [];
        
        foreach ((long vmId, VmType vmType) vm in availableVms)
        {
            switch (vm.vmType)
            {
                case VmType.VirtualMachine:
                    await CheckNewVm(node, vm.vmId, wolDevices, newDevices);
                    break;
                case VmType.Container:
                    await CheckNewContainer(node, vm.vmId, wolDevices, newDevices);
                    break;
                default:
                    throw new InvalidOperationException("I don't know how is possibile, but the type is neither vm nor container");
            }
        }

        if (!newDevices.Any())
        {
            _log.Info("No new vm/container found in proxmox");
            return;
        }
        await _dbContext.WolDevices.AddRangeAsync(newDevices);
        await _dbContext.SaveChangesAsync();
    }

    private async Task CheckNewVm(PveNodeItem node, long vmId, List<WolDevice> currentDevices, List<WolDevice> newDevices)
    {
        var vmConfig = await node.Qemu[vmId].Config.GetAsync();
        string? macAddress = vmConfig.Networks.FirstOrDefault()?.MacAddress;
        if (macAddress is null)
        {
            return;
        }
        
        //If it already exists, nothing to be done
        if (currentDevices.Any(d => d.DeviceMacAddress == macAddress))
        {
            return;
        }
        _log.Info("New virtual machine found: {name}-{macaddress}",vmConfig.Name, macAddress);
        WolDevice newDevice = new(macAddress, vmConfig.Name);
        newDevices.Add(newDevice);
    }
    
    private async Task CheckNewContainer(PveNodeItem node, long vmId, List<WolDevice> currentDevices, List<WolDevice> newDevices)
    {
        var vmConfig = await node.Lxc[vmId].Config.GetAsync();
        string? macAddress = vmConfig.Networks.FirstOrDefault()?.MacAddress;
        if (macAddress is null)
        {
            return;
        }
        
        //If it already exists, nothing to be done
        if (currentDevices.Any(d => d.DeviceMacAddress == macAddress))
        {
            return;
        }
        _log.Info("New container found: {name}-{macaddress}",vmConfig.Hostname, macAddress);
        WolDevice newDevice = new(macAddress, vmConfig.Hostname);
        newDevices.Add(newDevice);
    }
    
    private enum VmType
    {
        VirtualMachine = 0,
        Container
    }
}