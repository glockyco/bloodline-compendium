-- lab setup: loads static meshes to read their boxes and sockets, without changing anything
-- Lists the box and the sockets of each weapon part mesh in args.meshes, by which the game joins parts into a
-- weapon. capture-model.lua stands a part with its mesh's Z axis up, because every part mesh is modelled as it sits
-- in a weapon that points up that axis: the socket where a blade, a head or a shaft joins the handle lies at its
-- lower end, a pommel's and a wand focus's at its upper end, a grip's PommelSocket below its GuardSocket and a
-- guard's HandleSocket not above its BladeSocket. Pass the ComponentMesh paths of WeaponComponentTable and compare.
local out = {}
for _, path in ipairs(args.meshes) do
  local mesh = LoadAsset(path)
  if not mesh or not mesh:IsValid() then error("Mesh " .. path .. " did not load") end
  local box = mesh:GetBoundingBox()
  local row = {
    mesh = path,
    low = { box.Min.X, box.Min.Y, box.Min.Z },
    high = { box.Max.X, box.Max.Y, box.Max.Z },
    sockets = {},
  }
  mesh.Sockets:ForEach(function(_, element)
    local socket = element:get()
    if socket and socket:IsValid() then
      row.sockets[#row.sockets + 1] = {
        name = socket.SocketName:ToString(),
        at = { socket.RelativeLocation.X, socket.RelativeLocation.Y, socket.RelativeLocation.Z },
        turned = { socket.RelativeRotation.Pitch, socket.RelativeRotation.Yaw, socket.RelativeRotation.Roll },
      }
    end
  end)
  out[#out + 1] = row
end
return { meshes = out }
