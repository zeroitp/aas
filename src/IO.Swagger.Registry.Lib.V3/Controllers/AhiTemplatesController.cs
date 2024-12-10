namespace IO.Swagger.Controllers;

using System;
using System.Threading.Tasks;
using AasxServerStandardBib.Models;
using AasxServerStandardBib.Services;
using I.Swagger.Registry.Lib.V3.Services;
using IO.Swagger.Registry.Lib.V3.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("dev/templates")]
public class AhiTemplatesController(
    AssetAdministrationShellRepositoryAPIApiController aasRepoController,
    SubmodelRepositoryAPIApiController smRepoController,
    RuntimeAssetAttributeHandler runtimeAssetAttributeHandler,
    EventPublisher eventPublisher,
    TimeSeriesService timeSeriesService,
    AasApiHelperService aasApiHelper
) : ControllerBase
{
    [HttpPost]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.WRITE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates", Privileges.DeviceTemplate.Rights.WRITE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> AddAsync([FromBody] AddTemplates addEntity)
    {
        //var response = await _mediator.Send(addEntity);
        //return Created($"/dev/templates/{response.Id}", response);
        return Created();
    }

    [HttpPut("{id}")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.WRITE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/{id}", Privileges.DeviceTemplate.Rights.WRITE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateTemplates command)
    {
        //command.Id = id;
        //var response = await _mediator.Send(command);
        //return Accepted($"/dev/templates/{response.Id}", response);
        return Accepted();
    }

    [HttpGet("{id}")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/{id}", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> GetEntityByIdAsync(Guid id)
    {
        //var command = new GetTemplateByID(id);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("search")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/search", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> SearchAsync([FromBody] GetTemplateByCriteria command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/devices", Name = "GetDevicesByTemplateId")]
    public async Task<IActionResult> GetDevicesByTemplateId(Guid id)
    {
        //var command = new GetDevicesByTemplateId(id);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [Obsolete("This api will be removed in future due to the enhancement mentioned in USER STORY 1323")]
    [HttpGet]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> GetAllAsync()
    {
        //var response = await _mediator.Send(new GetTemplateByDefault());
        //return Ok(response);
        return Ok();
    }

    /// <summary>
    /// Search valid templates by using dynamic search condition.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost("valid/search")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/valid/search", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> GetTemplateByCriteriaAsync(GetValidTemplatesByCriteria command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/metrics")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/{id}/metrics", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> GetTemplateMetricsByTemplateIdAsync(Guid id, [FromQuery] bool isIncludeDisabledMetric = false)
    {
        //var response = await _mediator.Send(new GetTemplateMetricsByTemplateId(id, isIncludeDisabledMetric));
        //return Ok(response);
        return Ok();
    }

    [HttpDelete("{id}")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.DELETE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/{id}", Privileges.DeviceTemplate.Rights.DELETE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> DeleteForceAsync(Guid id)
    {
        //var command = new DeleteTemplates() { TemplateIds = new Guid[] { id } };
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpDelete]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.DELETE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates", Privileges.DeviceTemplate.Rights.DELETE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> DeleteForceListAsync([FromBody] DeleteTemplates command)
    {
        //BaseResponse response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("{id}/metrics/valid")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/{id}/metrics/valid", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> ValidateTemplateDetails([FromRoute] Guid id, [FromBody] ValidationTemplateDetails command)
    {
        //command.Id = id;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    
    [HttpPost("keytypes/search")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE, Privileges.DeviceTemplate.FullRights.WRITE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/keytypes/search", Privileges.DeviceTemplate.Rights.READ_DEVICE_TEMPLATE, Privileges.DeviceTemplate.Rights.WRITE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> SearchAsync([FromBody] GetTemplateKeyTypeByCriteria command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("export")]
    //[RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Configuration.ENTITY_NAME, "dev/templates/export", Privileges.Configuration.Rights.SHARE_CONFIGURATION)]
    public async Task<IActionResult> ExportDeviceTemplatesAsync([FromBody] ExportDeviceTemplate command)
    {
        //command.ObjectType = FileEntityConstants.DEVICE_TEMPLATE;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("import")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.WRITE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/import", Privileges.DeviceTemplate.Rights.WRITE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> ImportDeviceTemplatesAsync([FromBody] ImportFile command)
    {
        //command.ObjectType = FileEntityConstants.DEVICE_TEMPLATE;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("{id}/validate")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.WRITE_DEVICE_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.DeviceTemplate.ENTITY_NAME, "dev/templates/{id}/validate", Privileges.DeviceTemplate.Rights.WRITE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> ValidateExpressionAsync(Guid id, [FromBody] JsonPatchDocument patchDoc)
    {
        //var jsonPatch = new ValidateDeviceTemplateExpression
        //{
        //    DeviceTemplateId = id,
        //    Data = patchDoc,
        //    ValidateType = Application.Enum.ValidationType.DeviceTemplate
        //};
        //var response = await _mediator.Send<bool>(jsonPatch);
        //return Ok(response);
        return Ok();
    }

    [HttpHead("{id}")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE, Privileges.Device.FullRights.WRITE_DEVICE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, "", "dev/templates/{id}",
    //Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE,
    //     Privileges.Device.FullRights.WRITE_DEVICE)]
    public async Task<IActionResult> CheckExistTemplateAsync(Guid id)
    {
        //var command = new CheckExistTemplate(new Guid[] { id });
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("exist")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE, Privileges.Device.FullRights.WRITE_DEVICE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, "", "dev/templates/exist",
    //Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE,
    //   Privileges.Device.FullRights.WRITE_DEVICE)]
    public async Task<IActionResult> CheckExistTemplateAsync([FromBody] CheckExistTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/fetch")]
    public async Task<IActionResult> FetchAsync(Guid id)
    {
        //var command = new FetchTemplate(id);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("archive")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("retrieve")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.WRITE_DEVICE_TEMPLATE)]
    public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("archive/verify")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/metric/{metricId}/using")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> CheckMetricUsingAsync([FromRoute] Guid id, [FromRoute] Guid metricId)
    {
        //var command = new CheckMetricUsing(id, metricId);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/binding/{bindingId}/using")]
    //[RightsAuthorizeFilter(Privileges.DeviceTemplate.FullRights.READ_DEVICE_TEMPLATE)]
    public async Task<IActionResult> CheckBindingUsingAsync([FromRoute] Guid id, [FromRoute] int bindingId)
    {
        //var command = new CheckBindingUsing(id, bindingId);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }
}
