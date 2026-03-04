local exports = {}

local cpu = nil
local paused = false

local breakpointaddr = nil --0x8000
local breakpoint_hit = false

function exports.startplugin()
    print("Plugin ZXBS loaded!")

    --emu.register_start(function()
    emu.add_machine_reset_notifier(function()
        cpu = manager.machine.devices[":maincpu"]
        print("")
        print("CPU detected")

        if breakpointaddr then
            if cpu.debug then
                local id=cpu.debug:bpset(breakpointaddr, "", "")
                print("Breakpoint set " .. id)
            else
                print("DEBUG NO AVAILABLE")
            end
        end

        emu.register_periodic(timer)
    end)
end


function timer()
    if not cpu or not cpu.debug then 
        return
    end

    local current_pc = cpu.state["PC"].value
    if current_pc == breakpointaddr and breakpoint_hit == false then
        emu.pause()
        breakpoint_hit = true
        show_registers()
    end

    --if breakpoint_hit == true and current_pc != breakpointaddr then
    --    breakpoint_hit = false
    --end
end


function timer2()
    local cmd = get_command()

    if cmd == "PAUSE" then
        emu.pause()
        show_registers()
    else
        if cmd == nil then
            return
        end
        print("UNKNOW COMMAD")
        print(cmd)
    end
end


function get_command()
    local f = io.open("zxbs.tmp","r")
    if not f then
        return nil
    end
    local content = f:read("*a")
    f:close()
    os.remove("zxbs.tmp")
    return content
end


function show_registers()
    local pc = cpu.state["PC"].value
    local af = cpu.state["AF"].value
    local bc = cpu.state["BC"].value
    local de = cpu.state["DE"].value
    local hl = cpu.state["HL"].value
    print(string.format("BREAKPOINT PC:%04X AF:%04X BC:%04X DE:%04X HL:%04X",pc, af, bc, de, hl))        
end

return exports